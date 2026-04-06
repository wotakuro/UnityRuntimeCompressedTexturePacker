using UnityEngine;
using UnityEngine.UI;
using UTJ.RuntimeCompressedTexturePacker;

namespace UTJ.Sample
{

    /// <summary>
    /// Listアイテムのコンテナ
    /// </summary>
    public class IconItemComponent: MonoBehaviour
    {
        /// <summary>
        /// Sprit表示のImageオブジェクト
        /// </summary>
        [SerializeField]
        private Image imageBody;

        /// <summary>
        /// Sprit表示のImageオブジェクト
        /// </summary>
        [SerializeField]
        private Image loadingImage;


        // IconPath        
        private string iconPath;
        // IconPath        
        private string loadingIconPath;

        // Atlas生成用
        private RecycleAtlasForFixedSizeImages recycleAtlasForFixedSizeImages;

        // アイコンアイテムがスクロールインしてきたときのセットアップ処理
        public void BindItem(RecycleAtlasForFixedSizeImages recycleAtlas,string icon,string loadingIcon)
        {
            this.iconPath = icon;
            this.loadingIconPath = loadingIcon;
            this.recycleAtlasForFixedSizeImages = recycleAtlas;
        }

        // アイコンアイテムがスクロールアウトしたときの処理
        public void UnbindItem()
        {
            this.iconPath = null;
        }

        // 更新処理
        private void Update()
        {
            if (string.IsNullOrEmpty(this.iconPath) || this.recycleAtlasForFixedSizeImages == null)
            {
                return;
            }
            // Spriteをリクエスト、Imageオブジェクトの更新
            var loadingSprite = this.recycleAtlasForFixedSizeImages.Request(this.loadingIconPath);
            this.loadingImage.sprite = loadingSprite;
            var imageSprite = this.recycleAtlasForFixedSizeImages.Request(this.iconPath);
            this.imageBody.sprite = imageSprite;

            // 状態に合わせえて表示の有無を切り替え
            this.loadingImage.enabled = (loadingSprite != null ) &(imageSprite == null);
            this.imageBody.enabled = (imageSprite != null);

            // ロードアイコンの回転
            this.loadingImage.rectTransform.localRotation = Quaternion.Euler(0, 0, Time.timeSinceLevelLoad * 360.0f);
        }

#if UNITY_EDITOR
        public string GetIconName()
        {
            return System.IO.Path.GetFileName(this.iconPath);
        }
#endif

    }
}