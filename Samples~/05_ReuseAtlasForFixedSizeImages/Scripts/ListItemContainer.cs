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
        private ItemFunc bindItemFunc;


        // アイテムのセットアップ処理
        private ItemFunc unbindItemFunc;


        /// <summary>
        /// セットアップ処理
        /// </summary>
        /// <param name="prefab">対象のPrefab</param>
        /// <param name="scroll">対象のスクロールビュー</param>
        /// <param name="num">数</param>
        /// <param name="height"></param>
        /// <param name="topMargin"></param>
        /// <param name="bindFunc"></param>
        public void Setup(GameObject prefab, ScrollRect scroll, int num,int height,int topMargin,
            ItemFunc bindFunc,ItemFunc unbindFunc) {
            this.itemPrefab = prefab;
            this.scrollRect = scroll;
            this.itemNum = num;
            this.marginTop = topMargin;
            this.itemHeight = height;
            this.bindItemFunc = bindFunc;
            this.unbindItemFunc = unbindFunc;


            this.scrollRectTransform = this.scrollRect.GetComponent<RectTransform>();
            this.scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, num * itemHeight + marginTop);

            this.scrollRect.onValueChanged.AddListener(OnScrollChanged);

            int bufferNum = (int)(scrollRectTransform.rect.height / itemHeight) + 2;
            this.bufferedObject = new List<InstatntiateObject>(bufferNum);
            for (int i = 0; i < bufferNum; i++)
            {
                var obj = CreateInstanceObject(i);
                this.bufferedObject.Add(obj);
            }
        }

        /// <summary>
        /// 必要であるならバッファーの拡張を行います
        /// </summary>
        public void AppendBufferObjectIfNeeded()
        {
            int currentSize = (int)(scrollRectTransform.rect.height / itemHeight) + 2;
            for (int i = this.bufferedObject.Count; i < currentSize; i++)
            {
                var obj = CreateInstanceObject(i);
                this.bufferedObject.Add(obj);
            }
        }


        /// <summary>
        /// バッファ用のオブジェクトの追加処理を行います
        /// </summary>
        /// <param name="itemIndex">アイテムオブジェクト</param>
        /// <returns>追加したアイテム</returns>
        private InstatntiateObject CreateInstanceObject(int itemIndex)
        {
            var gmo = GameObject.Instantiate(this.itemPrefab);
            var rectTransform = gmo.GetComponent<RectTransform>();
            rectTransform.SetParent(this.scrollRect.content);
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = new Vector3(20, -marginTop - itemIndex * itemHeight, 0.0f);

            var obj = new InstatntiateObject()
            {
                gameObject = gmo,
                rectTransform = rectTransform,
                itemIndex = itemIndex,
                itemComponent = gmo.GetComponent<T>()
            };
            if (itemIndex >= 0){
                this.bindItemFunc(obj.itemComponent, itemIndex);
            }
            return obj;
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
            if (head < 0)
            {
                head = 0;
            }
            int tail = (int)(positionY + marginTop + scrollRectTransform.rect.height) / itemHeight;
            if(tail >= itemNum)
            {
                tail = itemNum - 1;
            }


            // 範囲外のモノを itemIndex -1でマークします
            for (int i = 0; i < this.bufferedObject.Count; i++)
            {
                var item = this.bufferedObject[i];
                if(item.itemIndex < 0)
                {
                    continue;
                }
                if (item.itemIndex < head || tail < item.itemIndex)
                {
                    if (this.unbindItemFunc != null)
                    {
                        this.unbindItemFunc(item.itemComponent, item.itemIndex);
                    }
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
                        this.bindItemFunc(itemObject.itemComponent, i);
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