using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UTJ.RuntimeCompressedTexturePacker;
using UTJ.RuntimeCompressedTexturePacker.Format;

namespace UTJ.Sample
{
    /// <summary>
    /// 暗号化ファイルテクスチャフォーマット（独自）
    /// </summary>
    public class EncryptedTextureFileFormat : ITextureFileFormat
    {
        // 先頭4ByteのSignature
        private const uint Signature = 0x5446594DU;
        // XORの暗号キー
        private const uint EncryptKey = 0x20534444U;

        // Textureの幅
        private uint textureWidth;
        // Textureの高さ
        private uint textureHeight;
        // Textureフォーマット
        private TextureFormat format;

        /// <summary>
        /// [interface 実装] Textureの幅
        /// </summary>
        public int width => (int)textureWidth;

        /// <summary>
        /// [interface 実装] Textureの高さ
        /// </summary>
        public int height => (int)textureHeight;

        /// <summary>
        /// [interface 実装] Textureのフォーマット
        /// </summary>
        public TextureFormat textureFormat => format;


        /// <summary>
        /// [interface 実装] Textureとして正しいか？
        /// </summary>
        public bool IsValid => (width > 0 && height >0);

        /// <summary>
        /// [interface 実装]ファイルの中身からMipmapではない形でのテクスチャの実態を返します
        /// </summary>
        /// <param name="fileBinary">ファイルの中身</param>
        /// <returns>作成したテクスチャを返します</returns>
        public NativeArray<byte> GeImageDataWithoutMipmap(NativeArray<byte> fileBinary)
        {
            NativeArray<byte> bytes = new NativeArray<byte>(fileBinary.Length - 16, Allocator.Temp);

            unsafe
            {
                uint* src = (uint*)(fileBinary.GetUnsafePtr()) + 4;
                uint*dst = (uint*)(bytes.GetUnsafePtr());
                int size = (fileBinary.Length-16 )/ 4;
                for (int i = 0; i < size; i++)
                {
                    *dst = *src ^ EncryptKey;
                    ++dst;++src;
                }
            }
            return bytes;
        }

        /// <summary>
        /// [interface 実装]ヘッダーのロード
        /// </summary>
        /// <param name="fileBinary">ファイルの中身</param>
        /// <returns>ロードに失敗ならFalseを返します</returns>
        public bool LoadHeader(NativeArray<byte> fileBinary)
        {
            if(fileBinary.Length < 16) { 
                return false;
            }
            this.textureWidth = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 4);
            this.textureHeight = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 8);
            this.format = (TextureFormat)BytesToOtherTypesUtility.ReadUintFast(fileBinary, 12);
            return true;
        }

        /// <summary>
        /// [interface 実装]Textureのロード
        /// </summary>
        /// <param name="fileBinary">ファイルの中身</param>
        /// <param name="isLinearColor">リニアカラーかどうか？</param>
        /// <param name="useMipmap">Mipmapも考慮するか？</param>
        /// <returns>作成されたTexture</returns>
        public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false)
        {
            // Mipmap考慮しないでテクスチャ作成します
            return TextureFileFormatUtility.CreateTextureWithoutMipmap(this,fileBinary,isLinearColor);
        }

        /// <summary>
        /// 先頭のバイトをみて、このフォーマットであるかを返します。
        /// </summary>
        /// <param name="fileBinary">ファイルデータのバイナリデータ</param>
        /// <returns>Signatureが一致しているならTrue</returns>
        public static bool SignatureValid(NativeArray<byte> fileBinary)
        {
            var signature = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 0);
            return (signature == Signature);
        }
    }
}