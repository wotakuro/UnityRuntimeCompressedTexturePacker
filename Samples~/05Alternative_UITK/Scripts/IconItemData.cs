using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UTJ.RuntimeCompressedTexturePacker;

namespace UTJ.Sample
{

    /// <summary>
    /// アイテム自体のデータ
    /// </summary>
    [Serializable]
    public class IconItemData
    {
        /// <summary>
        /// [DataBinding] アイテムアイコン用のSprite
        /// </summary>
        [SerializeField]
        public Sprite itemIconSprite;


        /// <summary>
        /// [DataBinding] ロードアイコン用のSprite
        /// </summary>
        [SerializeField]
        public Sprite loadingSprite;


        /// <summary>
        /// [DataBinding] ロード画像の回転
        /// </summary>
        [SerializeField]
        public Rotate loadingRotate;

        /// <summary>
        /// [DataBinding] アイテムの名前表示
        /// </summary>
        [SerializeField]
        public string itemName;


        /// Atlas作成用オブジェクト
        private RecycleAtlasForFixedSizeImages recycleAtlasForFixed;

        /// Item用のアイコン画像パス
        private string itemIconImagePath;

        /// Load用のアイコンの画像パス
        private string loadingIconImagePath;

        /// Bindされているか？
        private bool isBinding = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="recycleAtlasObj">Atlas作成用オブジェクト</param>
        /// <param name="itemPath">アイテムの画像パス</param>
        /// <param name="loadingPath">ロード用の画像パス</param>
        public IconItemData(RecycleAtlasForFixedSizeImages recycleAtlasObj, string itemPath,string loadingPath)
        {
            this.recycleAtlasForFixed = recycleAtlasObj;
            this.itemIconImagePath = itemPath;
            this.loadingIconImagePath = loadingPath;
            this.itemName = System.IO.Path.GetFileNameWithoutExtension(itemPath);

            this.loadingRotate = new Rotate(Angle.Degrees(0f) );
        }

        /// <summary>
        /// バインド時の処理
        /// </summary>
        public void OnBind()
        {
            isBinding = true;
        }

        /// <summary>
        /// バインドはがれた時の処理
        /// </summary>
        public void OnUnbind()
        {
            isBinding = false;
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        /// <param name="deltaTime">更新用のDeltaTime</param>
        public void OnUpdate(float deltaTime)
        {
            if (!isBinding)
            {
                return;
            }
            // ロード画像の回転処理
            this.loadingRotate.angle = this.loadingRotate.angle.value + deltaTime * 720;

            // Atlasのリクエスト＆セット
            if (this.recycleAtlasForFixed != null)
            {
                var loadingSpr = this.recycleAtlasForFixed.Request(this.loadingIconImagePath);
                var itemSpr = this.recycleAtlasForFixed.Request(this.itemIconImagePath);

                if (itemSpr)
                {
                    this.loadingSprite = null;
                }
                else
                {
                    this.loadingSprite = loadingSpr;
                }

                this.itemIconSprite = itemSpr;
            }
        }
    }
}