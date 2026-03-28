using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
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

        // セットアップを行います
        public void SetupInfo(RecycleAtlasForFixedSizeImages recycleAtlas,string icon,string loadingIcon)
        {
            this.iconPath = icon;
            this.loadingIconPath = loadingIcon;
            this.recycleAtlasForFixedSizeImages = recycleAtlas;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(this.iconPath) || this.recycleAtlasForFixedSizeImages == null)
            {
                return;
            }
            this.loadingImage.sprite = this.recycleAtlasForFixedSizeImages.Request(this.loadingIconPath);
            var imageSprite = this.recycleAtlasForFixedSizeImages.Request(this.iconPath);
            this.imageBody.sprite = imageSprite;

            this.loadingImage.rectTransform.localRotation = Quaternion.Euler(0, 0, Time.timeSinceLevelLoad * 360.0f);

            this.loadingImage.enabled = (imageSprite == null);
            this.imageBody.enabled = (imageSprite != null);
        }

#if UNITY_EDITOR
        public string GetIconName()
        {
            return System.IO.Path.GetFileName(this.iconPath);
        }
#endif

    }
}