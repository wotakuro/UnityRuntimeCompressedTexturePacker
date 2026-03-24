using System;
using UnityEngine;

namespace UTJ.RuntimeCompressedTexturePacker.Packing
{
    /// <summary>
    /// Textureパッキングを実際にどのように行うかを決めるアルゴリズム
    /// </summary>
    public class GridMapPacking : IRectResolveAlgorithm
    {
        /// <summary>
        /// 登録済みTextureを削除する事が出来るか？を返します
        /// </summary>
        public bool SupportRemove => true;
        
        /// <summary>
        /// Bitのフラグ
        /// </summary>
        private BitFlagCollection bitFlags;

        // Textureの幅
        private int textureWidth;
        // Textureの高さ
        private int textureHeight;
        // Gridの幅
        private int gridWidth;
        // Gridの高さ
        private int gridHeight;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="gridW">Gridの幅</param>
        /// <param name="gridH">Gridの高さ</param>
        public GridMapPacking(int gridW,int gridH)
        {
            this.gridWidth = gridW;
            this.gridHeight = gridH;
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="width">Packingされた結果が入るTextureの幅</param>
        /// <param name="height">Packingされた結果が入るTextureの高さ</param>
        public void Initialize(int width, int height)
        {
            this.textureWidth = width;
            this.textureHeight = height;

            if (this.bitFlags.IsDataCreated)
            {
                this.bitFlags.Dispose();
            }

            int num = (this.textureWidth / this.gridWidth) * (this.textureHeight / this.gridHeight);
            
            this.bitFlags = new BitFlagCollection(num);
        }

        /// <summary>
        /// 幅・高さを指定して、矩形を確保します
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="bestNode">確保された矩形を返します</param>
        /// <returns></returns>
        public bool Insert(int width, int height, out RectInt bestNode)
        {
            if (width > this.gridWidth || height > this.gridHeight)
            {
                bestNode = new RectInt(0, 0, 0, 0);
                return false;
            }
            int idx = this.bitFlags.FindFalseIndex();
            if (idx<0)
            {
                bestNode = new RectInt(0, 0, 0, 0);
                return false;
            }
            int xGridNum = (this.textureWidth / this.gridWidth);
            this.bitFlags.SetFlag(idx, true);

            bestNode = new RectInt( gridWidth * (idx % xGridNum ) , gridHeight * (idx/xGridNum),
                width,height);
            return true;
        }

        /// <summary>
        /// 削除処理を行います
        /// </summary>
        /// <param name="node">削除したい矩形を返します</param>
        /// <returns>削除に成功したかを返します</returns>
        public bool Remove(in RectInt node)
        {
            int xGrid = (node.x / this.gridWidth);
            int yGrid = (node.y / this.gridHeight);
            int xGridNum = (this.textureWidth / this.gridWidth);

            int idx = yGrid * xGridNum + xGrid;
            this.bitFlags.SetFlag(idx, false);

            return true;
        }

        /// <summary>
        /// Dispose処理
        /// </summary>
        public void Dispose()
        {
            this.bitFlags.Dispose();
        }
    }
}