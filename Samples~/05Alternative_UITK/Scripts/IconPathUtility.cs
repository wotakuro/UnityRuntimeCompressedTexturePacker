using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UTJ.RuntimeCompressedTexturePacker;
using UTJ.RuntimeCompressedTexturePacker.Format;

namespace UTJ.Sample
{

    /// <summary>
    /// アイテム自体のデータ
    /// </summary>
    public class IconPathUtility
    {
        //　ロードするアイコン画像名
        private const string LoadingIconName = "icon_loading";

        public static void GetLoadTextureInfo(out string[] iconPaths,out string loadingIconPath, out TextureFormat textureFormat)
        {
            var iconNames = GetIconImages();
            iconPaths = new string[iconNames.Length];
            loadingIconPath = null;
            textureFormat = TextureFormat.ARGB32;

            if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.ASTC_4x4))
            {
                textureFormat = TextureFormat.ASTC_4x4;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "astc/Icon/", "_4x4.astc");
                }
                loadingIconPath = GetAssetPath(LoadingIconName, "astc/Icon/", "_4x4.astc");
            }
            else if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.ETC2_RGBA8))
            {
                textureFormat = TextureFormat.ETC2_RGBA8;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "ktxEtc2RGBA8/Icon/", "_ETC2_RGBA.ktx");
                }
                loadingIconPath = GetAssetPath(LoadingIconName, "ktxEtc2RGBA8/Icon/", "_ETC2_RGBA.ktx");
            }
            else if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.BC7))
            {
                textureFormat = TextureFormat.BC7;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "ddsBC7/Icon/", "_BC7_UNORM.dds");
                }
                loadingIconPath = GetAssetPath(LoadingIconName, "ddsBC7/Icon/", "_BC7_UNORM.dds");
            }


        }
        // 実際に読み込むアイコン一覧取得
        private static string[] GetIconImages()
        {
            string[] iconNames = new string[49];
            for (int i = 1; i <= 49; ++i)
            {
                iconNames[i - 1] = string.Format("icon{0:000}", i);
            }
            return iconNames;
        }

        // 実際のAssetパスを取得
        private static string GetAssetPath(string name, string head, string tail)
        {
            return System.IO.Path.Combine(Application.streamingAssetsPath, head + name + tail);
        }

    }
}