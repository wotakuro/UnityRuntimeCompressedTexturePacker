using System.IO;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using System.Text;
using UnityEditor;
using UnityEngine.UIElements;
using System.Security.Policy;
using UTJ.RuntimeCompressedTexturePacker.Format;
using UTJ.RuntimeCompressedTexturePacker;

namespace UTJ.Sample
{
    /// <summary>
    /// ビルド時にStreamingAsset以下にあるファイルのリストテキストを作成
    /// </summary>
    public class TextureListSampleEditor : EditorWindow
    {
        /// <summary>
        /// Window作成
        /// </summary>
        [MenuItem("Samples/RuntimeCompressedTexturePacker/TextureListSampleEditor")]
        public static void Create()
        {
            TextureListSampleEditor wnd = GetWindow<TextureListSampleEditor>();
        }
        /// <summary>
        /// Enable時
        /// </summary>
        private void OnEnable()
        {
            // ScrollView to container the images
            var scrollView = new ScrollView();

            // Layout styling for the content container
            scrollView.contentContainer.style.flexDirection = FlexDirection.Row;
            scrollView.contentContainer.style.flexWrap = Wrap.Wrap;
            scrollView.contentContainer.style.paddingLeft = 10;
            scrollView.contentContainer.style.paddingTop = 10;
            scrollView.contentContainer.style.paddingRight = 10;
            scrollView.contentContainer.style.paddingBottom = 10;

            rootVisualElement.Add(scrollView);

            // Find all assets that contain sprites
            HashSet<string> paths = new HashSet<string>();
            var files = Directory.GetFiles(Application.streamingAssetsPath, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                if (file.EndsWith(".meta")) { continue; }
                using (var binFile = UnsafeFileReadUtility.LoadFileSync(file, Unity.Collections.Allocator.Temp))
                {
                    var name = Path.GetFileName(file);
                    var textureFormat = TextureFileFormatUtility.GetTextureFileFormatObject(binFile);
                    // Textureではない
                    if (textureFormat is NullTextureFile)
                    {
                        continue;
                    }

                    var texture = textureFormat.LoadTexture(binFile);
                    if (texture)
                    {
                        texture.name = name;
                        VisualElement visualElement = CreateImage(texture);
                        scrollView.Add(visualElement);
                    }
                }

            }
            // プレイステートが変わったら閉じる
            EditorApplication.playModeStateChanged += (state) =>
            {
                this.Close();
            };
        }

        /// <summary>
        /// VisualElementの作成
        /// </summary>
        /// <param name="texture">テクスチャ</param>
        /// <returns>作成したVisualElement</returns>
        private VisualElement CreateImage(Texture2D texture)
        {
            VisualElement visualElement = new VisualElement();
            var img = new Image();
            img.image = texture;

            // Styling for the image element
            img.style.width = 180;
            img.style.height = 180;
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

            visualElement.Add(new Label(texture.name));

            visualElement.style.marginRight = 8;
            visualElement.style.marginBottom = 8;
            return visualElement;
        }
    }
}