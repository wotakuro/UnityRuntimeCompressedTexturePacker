using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System;

namespace UTJ.RuntimeCompressedTexturePacker.Packing
{

    /// <summary>
    /// 空きスペースを探してくるMaximal Rectangles アルゴリズム
    /// 大小様々な矩形を効率よく隙間に埋めるのに適しています
    /// 削除操作には対応しません
    /// </summary>
    public class MaximalRectanglesPacking : IRectResolveAlgorithm
    {

        public bool SupportRemove => false;

        private List<RectInt> freeRectangles = new List<RectInt>(32);

        public void Initialize(int width, int height)
        {
            // 最初は全体が1つの空き矩形
            freeRectangles.Clear();
            freeRectangles.Add(new RectInt(0, 0, width, height));
        }

        /// 新しい矩形を挿入する
        public bool Insert(int width, int height, out RectInt bestNode)
        {
            bestNode = new RectInt();
            // 1. 最適な空きスペースを探す (Best Short Side Fit ヒューリスティック)
            int bestShortSideFit = int.MaxValue;
            int bestLongSideFit = int.MaxValue;
            bool found = false;

            for (int i = 0; i < freeRectangles.Count; i++)
            {
                RectInt free = freeRectangles[i];

                // 入るかチェック（回転は考慮しない版）
                if (free.width >= width && free.height >= height)
                {
                    int leftOverX = Mathf.Abs(free.width - width);
                    int leftOverY = Mathf.Abs(free.height - height);
                    int shortSideFit = Mathf.Min(leftOverX, leftOverY);
                    int longSideFit = Mathf.Max(leftOverX, leftOverY);

                    // より「ぴったり」な場所を優先する
                    if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                    {
                        bestNode = new RectInt(free.x, free.y, width, height);
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                        found = true;
                    }
                }
            }

            if (!found) return false; // 空きがない

            // 2. 矩形を配置し、空きスペースリストを更新する
            PlaceRect(bestNode);
            return true;
        }

        public bool Remove(in RectInt node)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 矩形を配置し、既存の空きスペースを分割する
        /// </summary>
        private void PlaceRect(RectInt usedRect)
        {
            int count = freeRectangles.Count;
            for (int i = 0; i < count; i++)
            {
                // 重なり判定を行い、重なっている空き矩形を分割する
                if (SplitFreeNode(freeRectangles[i], usedRect))
                {
                    // 分割が発生したら、元の大きな空き矩形は削除
                    RemoveFreeRectangle(i);
                }
            }
            // 不要な（内包された）空き矩形を削除する
            PruneFreeList();
        }

        /// <summary>
        /// 空き矩形(freeNode)が、配置された矩形(usedNode)と重なる場合、
        /// freeNodeを最大4つの新しい空き矩形に分割してリストに追加する。
        /// </summary>
        /// <returns>分割が発生したか</returns>
        private bool SplitFreeNode(RectInt freeNode, RectInt usedNode)
        {
            // 重なっていなければ何もしない
            if (usedNode.x >= freeNode.xMax || usedNode.xMax <= freeNode.x ||
                usedNode.y >= freeNode.yMax || usedNode.yMax <= freeNode.y)
                return false;

            // --- 4方向への分割 ---
            {
                RectInt newNode = freeNode;
                newNode.y = usedNode.yMax;
                newNode.height = freeNode.yMax - usedNode.yMax;
                AddFreeRectangle(newNode);
            }

            {
                RectInt newNode = freeNode;
                newNode.x = usedNode.xMax;
                newNode.width = freeNode.xMax - usedNode.xMax;
                AddFreeRectangle(newNode);
            }

            {
                RectInt newNode = freeNode;
                newNode.xMax = usedNode.x;
                AddFreeRectangle(newNode);
            }
            {
                RectInt newNode = freeNode;
                newNode.yMax = usedNode.y;
                AddFreeRectangle(newNode);
            }

            return true;
        }


        private bool AddFreeRectangle(in RectInt freeNode)
        {
            if (freeNode.width <= 0)
            {
                return false;
            }
            if (freeNode.height <= 0)
            {
                return false;
            }
            this.freeRectangles.Add(freeNode);
            return true;
        }

        /// <summary>
        /// 完全に他の空き矩形に含まれている矩形をリストから削除する
        /// これをしないとリストが爆発的に増える
        /// </summary>
        private void PruneFreeList()
        {
            for (int i = 0; i < freeRectangles.Count; ++i)
            {
                for (int j = i + 1; j < freeRectangles.Count; ++j)
                {
                    if (IsContainedIn(freeRectangles[i], freeRectangles[j]))
                    {
                        RemoveFreeRectangle(i);
                        --i;
                        break;
                    }
                    if (IsContainedIn(freeRectangles[j], freeRectangles[i]))
                    {
                        RemoveFreeRectangle(j);
                        --j;
                    }
                }
            }
        }

        private void RemoveFreeRectangle(int idx)
        {
            int count = freeRectangles.Count;
            freeRectangles[idx] = freeRectangles[count - 1];
            freeRectangles.RemoveAt(count - 1);
        }

        private bool IsContainedIn(RectInt a, RectInt b)
        {
            return (b.x <= a.x) && (a.xMax <= b.xMax) &&
                (b.y <= a.y) && (a.yMax <= b.yMax);
        }

#if DEBUG
        public string GetDebugInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
            foreach (var node in freeRectangles)
            {
                sb.Append(node.x).Append(",").Append(node.y).Append("  ").
                    Append(node.width).Append("x").Append(node.height).AppendLine();
            }
            return sb.ToString();
        }
#endif
    }
}