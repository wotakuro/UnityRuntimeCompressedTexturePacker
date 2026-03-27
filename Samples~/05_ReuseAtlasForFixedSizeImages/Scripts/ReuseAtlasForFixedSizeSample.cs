using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;
using UTJ.RuntimeCompressedTexturePacker;
using UTJ.RuntimeCompressedTexturePacker.Format;

namespace UTJ.Sample
{
    /// <summary>
    /// Atlas再利用型のサンプル
    /// </summary>
    public class ReuseAtlasForFixedSizeSample : MonoBehaviour
    {
        // アイテム追加するScrollRect
        [SerializeField]
        private ScrollRect scrollRect;
        // アイテムのPrefab
        [SerializeField]
        private GameObject itemPrefab;
        // BuildされたAtlasを表示する所
        [SerializeField]
        private RawImage buildAtlasImage;


        private RecycleAtlasForFixedSizeImages recycleAtlasForFixed;

        private ListItemContainer<IconItemComponent> iconItemsContainer;


        private const string LoadingIconName = "icon_loading";
        private string[] iconPaths;
        private string loadingIconPath;


        private void Awake()
        {

            var iconNames = GetIconImages();
            this.iconPaths = new string[iconNames.Length];
            TextureFormat textureFormat = TextureFormat.ARGB32;

            if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.ASTC_4x4))
            {
                textureFormat = TextureFormat.ASTC_4x4;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "astc/icon/", "_4x4.astc");
                }
                this.loadingIconPath = GetAssetPath(LoadingIconName, "astc/icon/", "_4x4.astc");
            }
            else if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.ETC2_RGBA8))
            {
                textureFormat = TextureFormat.ETC2_RGBA8;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "ktxEtc2RGBA8/icon/", "_ETC2_RGBA.ktx");
                }
                this.loadingIconPath = GetAssetPath(LoadingIconName, "ktxEtc2RGBA8/icon/", "_ETC2_RGBA.ktx");
            }
            else if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.BC7))
            {
                textureFormat = TextureFormat.BC7;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "ddsBC7/icon/", "_BC7_UNORM.dds");
                }
                this.loadingIconPath = GetAssetPath(LoadingIconName, "ddsBC7/icon/", "_BC7_UNORM.dds");
            }



            this.recycleAtlasForFixed = new RecycleAtlasForFixedSizeImages(1024, 1024, textureFormat, 256, 256);

            this.iconItemsContainer = new ListItemContainer<IconItemComponent>();
            this.iconItemsContainer.Setup(itemPrefab, scrollRect, iconPaths.Length, 160, 10, this.OnSetupItem);

        }

        private void Update()
        {
            if (this.recycleAtlasForFixed.texture2D)
            {
                buildAtlasImage.texture = this.recycleAtlasForFixed.texture2D;
            }
        }

        private void OnSetupItem( IconItemComponent item , int index)
        {
            item.SetupInfo(this.recycleAtlasForFixed, this.iconPaths[index],this.loadingIconPath);
        }

        private void OnDestroy()
        {
            if (recycleAtlasForFixed != null)
            {
                recycleAtlasForFixed.Dispose();
                recycleAtlasForFixed = null;
            }
        }

        private string GetAssetPath(string name,string head,string tail)
        {
            return System.IO.Path.Combine(Application.streamingAssetsPath, head + name + tail);
        }

        private string[] GetIconImages()
        {
            string[] iconNames = new string[49];
            for(int i = 1; i <= 49; ++i)
            {
                iconNames[i - 1] = string.Format("icon{0:000}", i);
            }
            return iconNames;
        }

    }
}