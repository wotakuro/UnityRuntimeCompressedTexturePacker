using Unity.Collections;
using UnityEngine;

namespace UTJ.RuntimeCompressedTexturePacker.Format
{
    /// <summary>
    /// TextureFile自体のフォーマット
    /// </summary>
    public interface ITextureFileFormat
    {
        /// <summary>
        /// テクスチャファイルの幅
        /// </summary>
        public int width { get; }
        /// <summary>
        /// Textureファイルの高さ
        /// </summary>
        public int height { get; }

        /// <summary>
        /// テクスチャフォーマット
        /// </summary>
        public TextureFormat textureFormat { get; }

        /// <summary>
        /// データが正しいかを返します。
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// ファイル全体を渡して、画像の実データ部分だけを切り抜いて返します。
        /// </summary>
        /// <param name="fileBinary">ファイル全体のバイナリデータ</param>
        /// <returns>Headerを除いた画像の実データ部分</returns>
        public NativeArray<byte> GeImageDataWithoutMipmap(NativeArray<byte> fileBinary);



        /// <summary>
        /// Headerのロード
        /// </summary>
        /// <param name="fileBinary">ファイルの中身</param>
        /// <returns>ロードの可否を返します</returns>
        public bool LoadHeader(NativeArray<byte> fileBinary);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileBinary"></param>
        /// <param name="isLinearColor"></param>
        /// <param name="useMipmap"></param>
        /// <returns></returns>
        public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false);

    }
}