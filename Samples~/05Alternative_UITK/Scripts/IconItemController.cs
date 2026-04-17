using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UTJ.RuntimeCompressedTexturePacker;

namespace UTJ.Sample
{


    /// <summary>
    /// アイテム自体のデータ
    /// </summary>
    public class IconItemController:MonoBehaviour
    {
        // 固定サイズの画像をAtlasを再利用しながら読み込むオブジェクト
        private RecycleAtlasForFixedSizeImages recycleAtlasForFixed;

        // アイテムデータリスト
        private List<IconItemData> itemDatas;

        // UIDocument
        private UIDocument document;

        // Atlas画像表示用
        private Image image;

        /// <summary>
        /// Enable時の処理
        /// </summary>
        void OnEnable()
        {

            string loadingIconPath;
            string[] iconDataPaths;
            TextureFormat textureFormat;
            IconPathUtility.GetLoadTextureInfo(out iconDataPaths, out loadingIconPath, out textureFormat);

            //  固定サイズの画像をAtlasを再利用しながら読み込むオブジェクトの作成
            this.recycleAtlasForFixed = new RecycleAtlasForFixedSizeImages(1024, 1024, textureFormat, 256, 256);

            // setup item
            this.itemDatas = new List<IconItemData>(iconDataPaths.Length);
            foreach(string iconDataPath in iconDataPaths)
            {
                var itemData = new IconItemData(recycleAtlasForFixed,iconDataPath,loadingIconPath);
                this.itemDatas.Add(itemData);
            }

            // UIのセットアップ
            this.document = this.GetComponent<UIDocument>();
            var root = document.rootVisualElement;
            var listView = root.Q<ListView>("ItemList");
            var atlasInfo = root.Q<Label>("AtlasTextureInfo");
            if (atlasInfo != null)
            {
                atlasInfo.text = "Atlas " + this.recycleAtlasForFixed.texture2D.width + "x" + this.recycleAtlasForFixed.texture2D.height + " " + textureFormat;
            }
            this.image = root.Q<Image>();

            // listViewのイベント登録
            listView.bindItem += (item, idx) =>
            {
                this.itemDatas[idx].OnBind();
            };
            listView.unbindItem += (item, idx) =>
            {
                this.itemDatas[idx].OnUnbind();
            };

            // listViewにBinding
            listView.itemsSource = this.itemDatas;
        }

        /// <summary>
        /// Disable時の処理
        /// </summary>
        private void OnDisable()
        {
            if (this.recycleAtlasForFixed != null)
            {
                this.recycleAtlasForFixed.Dispose();
                this.recycleAtlasForFixed = null;
            }
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update()
        {
            // アイテムのアップデート処理（SpriteのRequest及び ロードアイコン回転処理)
            if (itemDatas != null)
            {
                foreach (var item in itemDatas)
                {
                    item.OnUpdate(Time.deltaTime);
                }
            }
            // Textureのセット
            if(image != null)
            {
                image.image = this.recycleAtlasForFixed.texture2D;
            }
        }
    }
}