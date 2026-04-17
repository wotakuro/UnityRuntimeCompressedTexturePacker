using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UTJ.RuntimeCompressedTexturePacker;
using UTJ.RuntimeCompressedTexturePacker.Format;
using UnityEngine.UIElements;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using System.Reflection;

namespace UTJ.Sample
{
    /// <summary>
    /// Textureファイルを直接読み込むサンプル。
    /// </summary>
    public class SingleTextureLoadSampleEditor : EditorWindow
    {
        /// <summary>
        /// EditorWindowの作成
        /// </summary>
        [MenuItem("Samples/RuntimeCompressedTexturePacker/SingleTextureLoadSampleEditor")]
        public static void Create()
        {
            SingleTextureLoadSampleEditor.GetWindow<SingleTextureLoadSampleEditor>();
        }

        private Image image;
        private TextField pathTextField;

        /// <summary>
        /// Enable時
        /// </summary>
        private void OnEnable()
        {

            VisualElement fileSelectElement = new VisualElement();
            fileSelectElement.style.flexDirection = FlexDirection.Row;
            fileSelectElement.style.alignItems = Align.Center;
            fileSelectElement.style.paddingTop = 10;
            fileSelectElement.style.paddingLeft = 10;
            fileSelectElement.style.paddingRight = 10;

            var fileLabel = new Label("File:");
            fileLabel.style.width = 30;
            // ファイルパスを表示・直接入力するテキストフィールド
            this.pathTextField = new TextField();
            this.pathTextField.style.flexGrow = 1;
            this.pathTextField.style.marginRight = 5;
            this.pathTextField.value = System.IO.Path.Combine(Application.streamingAssetsPath, "astc/Banner/banner_beer_4x4.astc");

            // ファイルを選択するための参照ボタン
            Button browseButton = new Button();
            browseButton.text = "Browse...";
            browseButton.style.width = 60;

            // ボタンがクリックされた時の処理
            browseButton.clicked += () =>
            {
                string path = System.IO.Path.GetDirectoryName(pathTextField.value);
                string selectedPath = EditorUtility.OpenFilePanel("Select a file", path, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    this.pathTextField.value = selectedPath;
                }
            };
            fileSelectElement.Add(fileLabel);
            fileSelectElement.Add(this.pathTextField);

            this.rootVisualElement.Add(fileSelectElement);
            this.rootVisualElement.Add(browseButton);

            // Load Button
            var loadButton = new Button();
            loadButton.text = "Load Texture";
            loadButton.clicked += OnClickLoadButton;
            this.rootVisualElement.Add(loadButton);
            // Image
            this.image = new Image();


            this.image.style.borderTopWidth = 1;
            this.image.style.borderBottomWidth = 1;
            this.image.style.borderLeftWidth = 1;
            this.image.style.borderRightWidth = 1;
            this.image.style.borderTopColor = new Color(0.1f, 0.1f, 0.1f);
            this.image.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
            this.image.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
            this.image.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
            this.image.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            this.image.scaleMode = ScaleMode.ScaleToFit;

            this.rootVisualElement.Add(this.image);
        }

        private void OnClickLoadButton()
        {
            using(var fileBinary = UnsafeFileReadUtility.LoadFileSync(this.pathTextField.value, Allocator.Temp ) ){
                var textureFormatObj = TextureFileFormatUtility.GetTextureFileFormatObject( fileBinary );
                var texture = textureFormatObj.LoadTexture(fileBinary);
                this.image.image = texture;
            }
        }
    }
}