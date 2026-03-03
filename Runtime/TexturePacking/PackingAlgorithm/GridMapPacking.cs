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
        /// 初期化処理
        /// </summary>
        /// <param name="width">Packingされた結果が入るTextureの幅</param>
        /// <param name="height">Packingされた結果が入るTextureの高さ</param>
        public void Initialize(int width, int height)
        {

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
            bestNode = new RectInt();
            return true;
        }

        /// <summary>
        /// 削除処理を行います
        /// </summary>
        /// <param name="node">削除したい矩形を返します</param>
        /// <returns>削除に成功したかを返します</returns>
        public bool Remove(in RectInt node)
        {
            return true;
        }
    }
}