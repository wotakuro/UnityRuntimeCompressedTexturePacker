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
    public class ListItemContainer<T>  where T : MonoBehaviour
    {

        /// <summary>
        /// アイテムのセットアップ処理
        /// </summary>
        /// <param name="item">対象</param>
        /// <param name="index">何個目のアイテムか？</param>
        public delegate void ItemFunc(T item, int index);

        /// <summary>
        /// 生成されたオブジェクト
        /// </summary>
        struct InstatntiateObject
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public int itemIndex;
            public T itemComponent;
        }
        // Prefab
        private GameObject itemPrefab;
        // ScrollRect
        private ScrollRect scrollRect;
        // ScrollRectのRectTransform
        private RectTransform scrollRectTransform;
        // 生成したバッファオブジェクト
        private List<InstatntiateObject> bufferedObject;

        // Item Height
        private int itemHeight = 160;
        // トップのマージン
        private int marginTop = 10;
        // アイテム数
        private int itemNum;
        // アイテムのセットアップ処理
        private ItemFunc setupItemFunc;


        /// <summary>
        /// セットアップ処理
        /// </summary>
        /// <param name="prefab">対象のPrefab</param>
        /// <param name="scroll">対象のスクロールビュー</param>
        /// <param name="num">数</param>
        /// <param name="height"></param>
        /// <param name="topMargin"></param>
        /// <param name="setupFunc"></param>
        public void Setup(GameObject prefab, ScrollRect scroll, int num,int height,int topMargin,
            ItemFunc setupFunc) {
            this.itemPrefab = prefab;
            this.scrollRect = scroll;
            this.itemNum = num;
            this.marginTop = topMargin;
            this.itemHeight = height;
            this.setupItemFunc = setupFunc;


            this.scrollRectTransform = this.scrollRect.GetComponent<RectTransform>();
            this.scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, num * itemHeight + marginTop);

            this.scrollRect.onValueChanged.AddListener(OnScrollChanged);

            int bufferNum = (int)(scrollRectTransform.rect.height / itemHeight) + 2;
            this.bufferedObject = new List<InstatntiateObject>(bufferNum);
            for (int i = 0; i < bufferNum; i++)
            {

                var gmo = GameObject.Instantiate(itemPrefab);
                var rectTransform = gmo.GetComponent<RectTransform>();
                rectTransform.SetParent(this.scrollRect.content);
                rectTransform.localScale = Vector3.one;
                rectTransform.localPosition = new Vector3(20, - marginTop - i * itemHeight, 0.0f);

                var obj = new InstatntiateObject()
                {
                    gameObject = gmo,
                    rectTransform = rectTransform,
                    itemIndex = i,
                    itemComponent = gmo.GetComponent<T>()
                };
                this.setupItemFunc(obj.itemComponent, i);
                this.bufferedObject.Add(obj);
            }
        }

        /// <summary>
        ///  現在のBufferオブジェクトを削除します
        /// </summary>
        public void Destroy()
        {
            foreach(var obj in this.bufferedObject)
            {
                GameObject.Destroy(obj.gameObject);
            }
            this.bufferedObject.Clear();
        }
        /// <summary>
        /// Scrollが変わった時の処理
        /// </summary>
        /// <param name="position"></param>
        private void OnScrollChanged(Vector2 position)
        {
            float positionY = scrollRect.content.anchoredPosition.y;

            int head = (int)(positionY + marginTop) / itemHeight;
            int tail = (int)(positionY + marginTop + scrollRectTransform.rect.height) / itemHeight;

            // 範囲外のモノを itemIndex -1でマークします
            for (int i = 0; i < this.bufferedObject.Count; i++)
            {
                if (positionY > -this.bufferedObject[i].rectTransform.localPosition.y + itemHeight ||
                    positionY < -this.bufferedObject[i].rectTransform.localPosition.y + this.scrollRectTransform.rect.height)
                {
                    var item = this.bufferedObject[i];
                    item.itemIndex = -1;
                    this.bufferedObject[i] = item;
                }
            }

            for (int i = head; i <= tail && i < itemNum; ++i)
            {
                int item = GetBufferIndex(i);
                if (item < 0)
                {
                    int newItemIdx = GetBufferIndex(-1);
                    if (newItemIdx >= 0)
                    {
                        var itemObject = this.bufferedObject[newItemIdx];
                        itemObject.itemIndex = i;
                        setupItemFunc(itemObject.itemComponent, i);
                        this.bufferedObject[newItemIdx] = itemObject;
                        itemObject.rectTransform.localPosition = new Vector3(20, -10 - i * itemHeight);
                    }
                }
            }
        }
        /// <summary>
        /// バッファー内でItemIndexを元に探します
        /// </summary>
        /// <param name="idx">Index値</param>
        /// <returns>バッファー内のIndexを返します</returns>
        private int GetBufferIndex(int idx)
        {
            for (int i = 0; i < this.bufferedObject.Count; i++)
            {
                if (bufferedObject[i].itemIndex == idx)
                {
                    return i;
                }
            }
            return -1;
        }

    }
}