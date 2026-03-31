using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UTJ.RuntimeCompressedTexturePacker.Format;
using UTJ.RuntimeCompressedTexturePacker;
using System;
using System.Collections.Generic;
using Unity.Collections;
using System.Text;

namespace UTJ.Sample
{
    /// <summary>
    /// StreamingAssets以下にあるTextureを全て並べます
    /// </summary>
    public class TextureListSample : MonoBehaviour
    {
        // 読み込んだTextureを配置するScrollRect
        [SerializeField]
        private Text supportTextureInfo;

        // 読み込んだTextureを配置するScrollRect
        [SerializeField]
        private ScrollRect scrollRect;

        // デバッグ表示用のフォント
        [SerializeField]
        private Font debugFont;

        // これまでに追加した数
        private int appendTextureNum = 0;

        // Startメソッド
        private void Start()
        {
            this.SetupSupportTextureFormat();

            var files = GetStreamingAssetsFiles();

            foreach (var file in files)
            {
                if (file.EndsWith(".meta")) { continue; }
                using (var binFile = UnsafeFileReadUtility.LoadFileSync(file, Unity.Collections.Allocator.Temp))
                {
                    var name = Path.GetFileName(file);
                    var textureFormat = TextureFileFormatUtility.GetTextureFileFormatObject(binFile);
                    // Textureではない
                    if(textureFormat is NullTextureFile)
                    {
                        continue;
                    }

                    var texture = textureFormat.LoadTexture(binFile);
                    if (texture)
                    {
                        texture.name = name;
                        this.AddTextureToUI(texture);
                    }
                }
            }
        }

        /// <summary>
        /// サポートしているTextureFormat一覧
        /// </summary>
        private void SetupSupportTextureFormat()
        {
            if (!this.supportTextureInfo)
            {
                return;
            }
            var formats = new TextureFormat[]
            {
                TextureFormat.DXT1,TextureFormat.DXT5,TextureFormat.BC7,
                TextureFormat.ETC2_RGB,TextureFormat.ETC2_RGBA1,TextureFormat.ETC2_RGBA8,
                TextureFormat.ASTC_4x4,TextureFormat.ASTC_5x5,TextureFormat.ASTC_6x6,TextureFormat.ASTC_8x8,
                TextureFormat.ASTC_10x10,TextureFormat.ASTC_12x12,
            };
            StringBuilder sb = new StringBuilder();
            sb.Append("SupportFormat:");
            foreach (var format in formats)
            {
                if (TextureFileFormatUtility.IsSupportedTextureFormat(format))
                {
                    sb.Append(format).Append(",");
                }
            }
            this.supportTextureInfo.text = sb.ToString();
        }

        /// <summary>
        /// StreamingAsset以下のファイル一覧取得
        /// AndroidではBuild時に書き出される list.txtを用いてStreamingAsset以下を読みます。
        /// </summary>
        /// <returns>StreamingAssets以下のファイル一覧</returns>
        private string[] GetStreamingAssetsFiles()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string[] result;
            using (var bin = UnsafeFileReadUtility.LoadFileSync(Path.Combine(Application.streamingAssetsPath, "list.txt"), Allocator.Temp))
            {
                string str = System.Text.UTF8Encoding.UTF8.GetString(bin.AsReadOnlySpan());
                string[] lines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                result = new string[lines.Length];
                for(int i = 0; i < lines.Length; ++i)
                {
                    result[i] = Path.Combine(Application.streamingAssetsPath , lines[i]);
                }
                return result;
            }
#else
            return Directory.GetFiles(Application.streamingAssetsPath, "*", SearchOption.AllDirectories);
#endif
        }


        /// <summary>
        /// TextureをUI上に足します
        /// </summary>
        /// <param name="texture">テクスチャー</param>
        private void AddTextureToUI(Texture2D texture)
        {
            var spriteGmo = new GameObject("texture", typeof(RectTransform));
            var spriteRectTransform = spriteGmo.GetComponent<RectTransform>();
            spriteRectTransform.SetParent(this.scrollRect.content);

            int xNum = ((int)this.scrollRect.GetComponent<RectTransform>().rect.width - 10) / 210;

            float positionX = 5 + (appendTextureNum % xNum) * 210;
            float positionY = -5 - (appendTextureNum / xNum) * 230;

            float width = 200.0f;
            float height = 200.0f;

            if (texture.width > texture.height)
            {
                height = 200.0f * texture.height / texture.width;
            }
            else
            {
                width = 200.0f * texture.width / texture.height;
            }
            float offsetX = (200.0f - width) * 0.5f;
            float offsetY = (200.0f - height) * 0.5f;
            SetupRectTransformForDebugUI(spriteRectTransform, width, height,
                positionX + offsetX, positionY - offsetY);
            var img = spriteGmo.AddComponent<RawImage>();
            img.texture = texture;

            // Add Text
            var textGmo = new GameObject("info", typeof(RectTransform));
            var textRectTransform = textGmo.GetComponent<RectTransform>();
            textRectTransform.SetParent(spriteRectTransform);
            SetupRectTransformForDebugUI(textRectTransform, 180.0f, 20.0f, 4 - offsetX, -210 + offsetY);
            var text = textGmo.AddComponent<Text>();
            text.text = texture.name;
            text.font = debugFont;
            text.color = Color.black;


            this.appendTextureNum++;
            this.scrollRect.content.sizeDelta = new Vector2(this.scrollRect.content.sizeDelta.x, -positionY + 230);
        }
        // Debug用Sprite表示のRectTransformのセットアップ
        private void SetupRectTransformForDebugUI(RectTransform rectTransform, float width, float height, float positionX, float positionY)
        {
            rectTransform.anchoredPosition = new Vector2(0, 0);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.localPosition = new Vector2(positionX, positionY);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }



    }

}