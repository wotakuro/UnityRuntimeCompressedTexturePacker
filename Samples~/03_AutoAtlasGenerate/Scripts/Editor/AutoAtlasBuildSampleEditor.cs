using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UTJ.RuntimeCompressedTexturePacker;
using UnityEditor;
using UnityEngine.UIElements;

namespace UTJ.Sample
{

    /// <summary>
    /// AutoAtlasBuilderを用いてASTCファイルをLoad & Packingを行う処理
    /// </summary>
    public class AutoAtlasBuildSampleEditor : EditorWindow
    {
        // Dropダウンのテクスチャタイプ
        private enum DropdownTextureType: int
        {
            ASTC = 0,
            KTX = 1,
            DDS = 2,
        }


        [MenuItem("Samples/RuntimeCompressedTexturePacker/AutoAtlasBuildSampleEditor")]
        public static void Create()
        {
            AutoAtlasBuildSampleEditor wnd = GetWindow<AutoAtlasBuildSampleEditor>();
        }
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
        // Enumのドロップダウンメニュー
        private EnumField enumDropdown;
        // テクスチャファイルの読み込みと、Packingを自動で任せます
        private AutoAtlasBuilder autoAtlasBuilder;

        private Image rowAtlas;
        private ScrollView spriteListScrollView;

        private void OnEnable()
        {
            this.enumDropdown = new EnumField("Texture Type", DropdownTextureType.ASTC);
            this.rootVisualElement.Add(enumDropdown);
            var button = new Button();
            button.text = "Load And Pack";
            button.clicked += this.OnClickButton;
            button.style.marginBottom = 10;
            this.rootVisualElement.Add(button);

            this.rootVisualElement.Add(new Label("Atlas Texture"));
            // atlas image
            this.rowAtlas = new Image();
            this.rowAtlas.style.width = 200;
            this.rowAtlas.style.height = 200;
            this.rowAtlas.style.minHeight = 200;
            this.rowAtlas.style.backgroundColor = Color.white;
            this.rowAtlas.style.marginBottom = 20;
            this.rootVisualElement.Add(rowAtlas);

            //
            this.rootVisualElement.Add(new Label("Generaged Sprites"));
            // scroll
            this.spriteListScrollView = new ScrollView();
            this.spriteListScrollView.contentContainer.style.flexDirection = FlexDirection.Row;
            this.spriteListScrollView.contentContainer.style.flexWrap = Wrap.Wrap;
            this.spriteListScrollView.contentContainer.style.paddingTop = 10;
            this.spriteListScrollView.contentContainer.style.paddingRight = 10;
            this.spriteListScrollView.contentContainer.style.paddingBottom = 10;

            this.rootVisualElement.Add(this.spriteListScrollView);

            // プレイステートが変わったら閉じる
            EditorApplication.playModeStateChanged += (state) =>
            {
                this.Close();
            };

        }

        /// <summary>
        /// ボタンがクリックされたとき
        /// </summary>
        private void OnClickButton()
        {
            this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, targetTextureFormat);
            var sprites = autoAtlasBuilder.LoadAndPack(this.targetTextureFiles);
            this.spriteListScrollView.Clear();
            foreach (var sprite in sprites)
            {
                var element = CreateImage(sprite);
                this.spriteListScrollView.Add(element);
            }

            this.rowAtlas.image = autoAtlasBuilder.texture;
        }

        /// <summary>
        /// Disable時処理
        /// </summary>
        private void OnDisable()
        {
            if (this.autoAtlasBuilder != null)
            {
                this.autoAtlasBuilder.DestroyTextureImmediate();
                this.autoAtlasBuilder.Dispose();
            }
        }

        /// <summary>
        /// Dropdownを考慮して、実際に読み込むTextureファイル
        /// </summary>
        private string[] targetTextureFiles
        {
            get
            {
                if (enumDropdown != null)
                {
                    switch (enumDropdown.value)
                    {
                        case DropdownTextureType.ASTC:
                            return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "astc/", "_4x4.astc");
                        case DropdownTextureType.KTX:
                            return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "ktxEtc2RGBA8/", "_ETC2_RGBA.ktx");
                        case DropdownTextureType.DDS:
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
                if (enumDropdown != null)
                {
                    switch (enumDropdown.value)
                    {
                        case DropdownTextureType.ASTC:
                            return TextureFormat.ASTC_4x4;
                        case DropdownTextureType.KTX:
                            return TextureFormat.ETC2_RGBA8;
                        case DropdownTextureType.DDS:
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



        /// <summary>
        /// VisualElementの作成
        /// </summary>
        /// <param name="texture">テクスチャ</param>
        /// <returns>作成したVisualElement</returns>
        private VisualElement CreateImage(Sprite spr)
        {
            VisualElement visualElement = new VisualElement();
            var img = new Image();
            img.sprite = spr;

            // Styling for the image element
            img.style.width = 120;
            img.style.height = 120;
            img.style.borderTopWidth = 1;
            img.style.borderBottomWidth = 1;
            img.style.borderLeftWidth = 1;
            img.style.borderRightWidth = 1;
            img.style.borderTopColor = new Color(0.1f, 0.1f, 0.1f);
            img.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
            img.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
            img.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
            img.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            // Fit the sprite into the image container
            img.scaleMode = ScaleMode.ScaleToFit;

            visualElement.Add(img);

            visualElement.Add(new Label(spr.name));

            visualElement.style.marginRight = 8;
            visualElement.style.marginBottom = 8;
            return visualElement;
        }
    }

}