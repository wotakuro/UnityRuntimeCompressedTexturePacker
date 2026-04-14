using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UTJ.RuntimeCompressedTexturePacker;
using System.Collections;


namespace UTJ.Sample
{

    /// <summary>
    /// AutoAtlasBuilderを用いてASTCファイルをLoad & Packingを行う処理
    /// </summary>
    public class IncrementalAtlasBuildSample : MonoBehaviour
    {

        // Dropダウンのテクスチャタイプ
        private enum DropdownTextureType : int
        {
            ASTC = 0,
            KTX = 1,
            DDS = 2,
        }
        /// <summary>
        /// TextureTypeのドロップダウン
        /// </summary>
        [SerializeField]
        private Dropdown textureTypeDropdown;

        /// <summary>
        /// 生成したSprite一覧を出すところ
        /// </summary>
        [SerializeField]
        private ScrollRect scrollRect;

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
            "Sprites/Sprite_Beer",
            "Sprites/Sprite_Fork",
            "Sprites/Sprite_Gradationbar",
            "Sprites/Sprite_Gradationbase",
            "Sprites/Sprite_Kimbap",
            "Sprites/Sprite_Knife",
            "Sprites/Sprite_Mustard",
            "Sprites/Sprite_SoySaurce",
            "Sprites/Sprite_Spoon",
            "Sprites/Sprite_Sticks",
            "Sprites/Sprite_TomatoSauce",
        };

        /// <summary>
        /// Dropdownを考慮して、実際に読み込むTextureファイル
        /// </summary>
        private string[] targetTextureFiles
        {
            get
            {
                if (textureTypeDropdown)
                {
                    switch (textureTypeDropdown.value)
                    {
                        case (int)DropdownTextureType.ASTC:
                            return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "astc/", "_4x4.astc");
                        case (int)DropdownTextureType.KTX:
                            return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "ktxEtc2RGBA8/", "_ETC2_RGBA.ktx");
                        case (int)DropdownTextureType.DDS:
                            return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "ddsBC7/", "_BC7_UNORM.dds");
                    }
                }
                return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "astc/", "_4x4.astc");
            }
        }

        /// <summary>
        /// 対象のTextureFormat
        /// </summary>
        private TextureFormat targetTextureFormat
        {
            get
            {
                if (textureTypeDropdown)
                {
                    switch (textureTypeDropdown.value)
                    {
                        case (int)DropdownTextureType.ASTC:
                            return TextureFormat.ASTC_4x4;
                        case (int)DropdownTextureType.KTX:
                            return TextureFormat.ETC2_RGBA8;
                        case (int)DropdownTextureType.DDS:
                            return TextureFormat.BC7;
                    }
                }
                return TextureFormat.ASTC_4x4;
            }
        }

        /// 実際にロードするファイルを求めます
        private string[] GetLoadFileList(string[] files, string baseDir, string head, string tail)
        {
            string[] output = new string[files.Length];
            for (int i = 0; i < files.Length; ++i)
            {
                output[i] = System.IO.Path.Combine(baseDir, head + files[i]) + tail;
            }
            return output;
        }

        // テクスチャファイルの読み込みと、Packingを自動で任せます
        private AutoAtlasBuilder autoAtlasBuilder;

        // 生成したSpriteを配置する場所
        private float spritePositionY = -5;


        // Awake時にTextureFormatサポート状況を見て Dropdownの値を決めます
        private void Awake()
        {
            if (textureTypeDropdown)
            {
                if (SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_4x4))
                {
                    textureTypeDropdown.value = 0;
                }
                else if (SystemInfo.SupportsTextureFormat(TextureFormat.ETC2_RGBA8))
                {
                    textureTypeDropdown.value = 1;
                }
                else if (SystemInfo.SupportsTextureFormat(TextureFormat.BC7))
                {
                    textureTypeDropdown.value = 2;
                }
            }
        }

        /// <summary>
        /// 非同期ロードの開始
        /// </summary>
        public void OnClickIncrementalBuild()
        {
            this.StartCoroutine(StartIncrementalAtlasBuild());
        }

        private IEnumerator StartIncrementalAtlasBuild()
        {
            // 既に作成済みの場合削除
            if(this.autoAtlasBuilder != null)
            {
                this.autoAtlasBuilder.Dispose();
            }
            this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, targetTextureFormat);
            var loadFiles = targetTextureFiles;
            string[] currentTargetFile = new string[1];

            foreach (var file in loadFiles)
            {
                currentTargetFile[0] = file;
                var enumerator = this.autoAtlasBuilder.LoadAndPackAsyncCoroutine(currentTargetFile, this.OnCompleteLoadAndPack, this.OnFailedLoadFile);
                while (enumerator.MoveNext())
                {
                    yield return null;
                }
                yield return new WaitForSeconds(1.0f);
            }
            this.autoAtlasBuilder.ReleaseBuffers();
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
            this.scrollRect.content.sizeDelta = new Vector2(190.0f, -spritePositionY);
        }

        /// <summary>
        /// Textureファイル読みこみ失敗、Packing失敗時に呼び出されます
        /// </summary>
        /// <param name="file">失敗したファイル</param>
        /// <param name="width">失敗したTextureの幅</param>
        /// <param name="height">失敗したTextureの高さ</param>
        private void OnFailedLoadFile(string file, AtlasFailedReason reason, int width, int height)
        {
            Debug.LogError("Failed LoadFile " + file + "::" + reason+ "::" + width + "x" + height);
        }



        /// <summary>
        /// デバッグ用UIにSpriteを追加
        /// </summary>
        /// <param name="sprite">追加されたスプライト</param>

        private void AddSpriteToUI(Sprite sprite)
        {
            var spriteGmo = new GameObject("spirte", typeof(RectTransform));
            var spriteRectTransform = spriteGmo.GetComponent<RectTransform>();
            spriteRectTransform.SetParent(scrollRect.content);


            float height = 0.0f;
            float width = 0.0f;
            if (sprite.rect.height > sprite.rect.width)
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
            SetupRectTransformForDebugUI(textRectTransform, 180.0f, 20.0f, -height - 5);
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

            this.scrollRect.velocity = new Vector2(0, height * 10 );
        }
        // Debug用Sprite表示のRectTransformのセットアップ
        private void SetupRectTransformForDebugUI(RectTransform rectTransform, float width, float height, float positionY)
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