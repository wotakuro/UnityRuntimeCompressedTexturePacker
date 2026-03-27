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
        /// テキスト
        /// </summary>
        [SerializeField]
        private Text text;

        // IconPath        
        private string iconPath;

        // Atlas生成用
        private RecycleAtlasForFixedSizeImages recycleAtlasForFixedSizeImages;

        // セットアップを行います
        public void SetupInfo(RecycleAtlasForFixedSizeImages recycleAtlas,string path)
        {
            this.iconPath = path;
            this.recycleAtlasForFixedSizeImages = recycleAtlas;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(this.iconPath) || this.recycleAtlasForFixedSizeImages == null)
            {
                return;
            }
            this.imageBody.sprite = this.recycleAtlasForFixedSizeImages.Request(this.iconPath);
        }

#if UNITY_EDITOR
        public string GetIconName()
        {
            return System.IO.Path.GetFileName(this.iconPath);
        }
#endif

    }
}