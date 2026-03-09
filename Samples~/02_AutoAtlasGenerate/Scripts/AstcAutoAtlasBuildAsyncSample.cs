using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UTJ.RuntimeCompressedTexturePacker;

namespace UTJ.Sample
{

    /// <summary>
    /// AutoAtlasBuilderを用いてASTCファイルをLoad & Packingを行う処理
    /// </summary>
    public class AstcAutoAtlasBuildAsyncSample : MonoBehaviour
    {
        /// <summary>
        /// 生成したSprite一覧を出すところ
        /// </summary>
        [SerializeField]
        private RectTransform spriteList;

        /// <summary>
        /// PackされたAtlasTextureを表示する所
        /// </summary>
        [SerializeField]
        private RawImage rawImage;

        /// <summary>
        /// デバッグ表示で利用するフォント
        /// </summary>
        [SerializeField]
        private Font debugTextFont;

        /// <summary>
        /// 生成したSpriteをInspector上で確認するためだけに用意
        /// </summary>
        [Header("For debug in Inspector")]
        [SerializeField]
        public List<Sprite> spriteListForDebug = new List<Sprite>();


        /// <summary>
        /// ロードするファイル(Androidでは、StreamingAssetsはDirectory.GetFilesで取得できないのもあって直書き
        /// </summary>
        private string[] loadFilesInStreamingAssets = {
            "astc/Sprites/Sprite_Beer_4x4.astc",
            "astc/Sprites/Sprite_Fork_4x4.astc",
            "astc/Sprites/Sprite_Gradationbar_4x4.astc",
            "astc/Sprites/Sprite_Gradationbase_4x4.astc",
            "astc/Sprites/Sprite_Kimbap_4x4.astc",
            "astc/Sprites/Sprite_Knife_4x4.astc",
            "astc/Sprites/Sprite_Mustard _4x4.astc",
            "astc/Sprites/Sprite_SoySaurce_4x4.astc",
            "astc/Sprites/Sprite_Spoon_4x4.astc",
            "astc/Sprites/Sprite_Sticks_4x4.astc",
            "astc/Sprites/Sprite_TomatoSauce_4x4.astc",
        };

        // テクスチャファイルの読み込みと、Packingを自動で任せます
        private AutoAtlasBuilder autoAtlasBuilder;

        // 生成したSpriteを配置する場所
        private float spritePositionY =-5;

        /// <summary>
        /// 非同期ロードの開始
        /// </summary>
        public void AsyncLoadStart()
        {
            this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, TextureFormat.ASTC_4x4);
            var loadFiles = new List<string>(loadFilesInStreamingAssets.Length);

            foreach(var file in loadFilesInStreamingAssets)
            {
                loadFiles.Add( System.IO.Path.Combine(Application.streamingAssetsPath, file)); 
            }
            // ランダム順にして実験したい場合
            // var randomOrder = loadFiles.OrderBy(x => System.Guid.NewGuid());

            // コルーチンでLoadAndPackAsyncCoroutine を実行することで非同期読み込みになります
            this.StartCoroutine(autoAtlasBuilder.LoadAndPackAsyncCoroutine(loadFiles, this.OnCompleteLoadAndPack, OnFailedLoadFile));
        }

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        private void OnDestroy()
        {
            if (autoAtlasBuilder != null)
            {
                // This is not required. You can destroy the texture object at your timing.
                // 必須ではないですが、任意のタイミングでTexture破棄したいなら四で下し亜
                if (autoAtlasBuilder.texture)
                {
                    Object.Destroy(autoAtlasBuilder.texture);
                }
                // Dispose処理
                autoAtlasBuilder.Dispose();
            }
        }

        /// <summary>
        /// LoadとSprite生成が終わったタイミングで呼び出されます
        /// </summary>
        /// <param name="sprites">生成されたSprite</param>
        private void OnCompleteLoadAndPack(IEnumerable<Sprite> sprites)
        {
            this.rawImage.texture = autoAtlasBuilder.texture;
            foreach (var sprite in sprites)
            {
                this.AddSpriteToUI(sprite);
                this.spriteListForDebug.Add(sprite);
            }
            this.spriteList.sizeDelta = new Vector2(190.0f, -spritePositionY);
            this.autoAtlasBuilder.ReleaseBuffers();
        }

        /// <summary>
        /// Textureファイル読みこみ失敗、Packing失敗時に呼び出されます
        /// </summary>
        /// <param name="file">失敗したファイル</param>
        /// <param name="width">失敗したTextureの幅</param>
        /// <param name="height">失敗したTextureの高さ</param>
        private void OnFailedLoadFile(string file, int width, int height)
        {
            Debug.LogError("Failed LoadFile " + file + "::" + width + "x" + height);
        }



        /// <summary>
        /// デバッグ用UIにSpriteを追加
        /// </summary>
        /// <param name="sprite">追加されたスプライト</param>

        private void AddSpriteToUI(Sprite sprite)
        {
            var spriteGmo = new GameObject("spirte", typeof(RectTransform));
            var spriteRectTransform = spriteGmo.GetComponent<RectTransform>();
            spriteRectTransform.SetParent(spriteList);


            float height = 0.0f; 
            float width = 0.0f; 
            if(sprite.rect.height > sprite.rect.width)
            {
                height = Mathf.Min(100.0f, sprite.rect.height);
                width = height * sprite.rect.width / sprite.rect.height;
            }
            else
            {
                width = Mathf.Min(100.0f, sprite.rect.width);
                height = width * sprite.rect.height / sprite.rect.width;
            }
            SetupRectTransformForDebugUI(spriteRectTransform, width, height, spritePositionY);
            var img = spriteGmo.AddComponent<Image>();
            img.sprite = sprite;

            // Add Text
            var textGmo = new GameObject("info", typeof(RectTransform));
            var textRectTransform = textGmo.GetComponent<RectTransform>();
            textRectTransform.SetParent(spriteRectTransform);
            SetupRectTransformForDebugUI(textRectTransform, 180.0f, 20.0f, -height-5);
            var text = textGmo.AddComponent<Text>();
            text.text = sprite.name;
            text.font = debugTextFont;
            text.color = Color.black;
            // Add border line
            var lineGmo = new GameObject("line", typeof(RectTransform));
            var lineRectTransform = lineGmo.GetComponent<RectTransform>();
            lineRectTransform.SetParent(spriteRectTransform);
            SetupRectTransformForDebugUI(lineRectTransform, 180.0f, 2.0f, -height - 26);
            var lineImg = lineGmo.AddComponent<Image>();
            lineImg.color = Color.black;

            spritePositionY -= (height + 30);
        }
        // Debug用Sprite表示のRectTransformのセットアップ
        private void SetupRectTransformForDebugUI(RectTransform rectTransform,float width , float height,float positionY)
        {
            rectTransform.anchoredPosition = new Vector2(0, 0);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.localPosition = new Vector2(5, positionY);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }


    }

}