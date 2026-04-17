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

        // TextureFormatの所
        [SerializeField]
        private Text textureFormatInfo;

        // 固定サイズの画像をAtlasを再利用しながら読み込むオブジェクト
        private RecycleAtlasForFixedSizeImages recycleAtlasForFixed;

        // アイコン表示しているアイテムオブジェクト（再利用するバッファ）
        private ListItemContainer<IconItemComponent> iconItemsContainer;

        //　ロードするアイコン画像名
        private const string LoadingIconName = "icon_loading";

        // 実際に読み込むロードアイコンの画像パス
        private string loadingIconPath;
        // 実際に読み込むアイコン画像のパス
        private string[] iconPaths;

        // 初期化処理
        private void Awake()
        {
            // 実際のファイルパス、画像フォーマット等の決定
            var iconNames = GetIconImages();
            this.iconPaths = new string[iconNames.Length];
            TextureFormat textureFormat = TextureFormat.ARGB32;

            if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.ASTC_4x4))
            {
                textureFormat = TextureFormat.ASTC_4x4;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "astc/Icon/", "_4x4.astc");
                }
                this.loadingIconPath = GetAssetPath(LoadingIconName, "astc/Icon/", "_4x4.astc");
            }
            else if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.ETC2_RGBA8))
            {
                textureFormat = TextureFormat.ETC2_RGBA8;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "ktxEtc2RGBA8/Icon/", "_ETC2_RGBA.ktx");
                }
                this.loadingIconPath = GetAssetPath(LoadingIconName, "ktxEtc2RGBA8/Icon/", "_ETC2_RGBA.ktx");
            }
            else if (TextureFileFormatUtility.IsSupportedTextureFormat(TextureFormat.BC7))
            {
                textureFormat = TextureFormat.BC7;
                for (int i = 0; i < iconNames.Length; ++i)
                {
                    iconPaths[i] = GetAssetPath(iconNames[i], "ddsBC7/Icon/", "_BC7_UNORM.dds");
                }
                this.loadingIconPath = GetAssetPath(LoadingIconName, "ddsBC7/Icon/", "_BC7_UNORM.dds");
            }

            // RecycleAtlasオブジェクトの作成
            this.recycleAtlasForFixed = new RecycleAtlasForFixedSizeImages(1024, 1024, textureFormat, 256, 256);

            // アイコン表示のセットアップ
            this.iconItemsContainer = new ListItemContainer<IconItemComponent>();
            this.iconItemsContainer.Setup(itemPrefab, scrollRect, iconPaths.Length, 160, 10, this.OnBindItem, this.OnUnbindItem);

            // 画像のセットアップ
            if (textureFormatInfo)
            {
                textureFormatInfo.text = "AtlasTexture "+ textureFormat.ToString();
            }
        }

        // 更新処理
        private void Update()
        {
            // デバッグ表示用のAtlasテクスチャ表示を設定
            if (this.recycleAtlasForFixed.texture2D)
            {
                buildAtlasImage.texture = this.recycleAtlasForFixed.texture2D;
            }
            // 画面比率が変わった等があった時にアイテムを追加します
            if(iconItemsContainer != null)
            {
                iconItemsContainer.AppendBufferObjectIfNeeded();
            }
        }

        // アイテムがスクロールインして表示される時に呼び出されるコールバック
        private void OnBindItem( IconItemComponent item , int index)
        {
            item.BindItem(this.recycleAtlasForFixed, this.iconPaths[index],this.loadingIconPath);
        }

        // アイテムがスクロールアウトして表示される時に呼び出されるコールバック
        private void OnUnbindItem(IconItemComponent item, int index)
        {
            item.UnbindItem();
        }

        // 破棄時の処理
        private void OnDestroy()
        {
            if (recycleAtlasForFixed != null)
            {
                recycleAtlasForFixed.Dispose();
                recycleAtlasForFixed = null;
            }
        }

        // 実際のAssetパスを取得
        private string GetAssetPath(string name,string head,string tail)
        {
            return System.IO.Path.Combine(Application.streamingAssetsPath, head + name + tail);
        }

        // 実際に読み込むアイコン一覧取得
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