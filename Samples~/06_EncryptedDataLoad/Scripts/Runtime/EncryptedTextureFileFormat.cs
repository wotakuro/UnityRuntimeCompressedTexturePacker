using System.Globalization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UTJ.RuntimeCompressedTexturePacker;
using UTJ.RuntimeCompressedTexturePacker.Format;

namespace UTJ.Sample
{
    /// <summary>
    /// サンプルコード
    /// </summary>
    public class EncryptedTextureFileFormat : ITextureFileFormat
    {
        private const uint Signature = 0x5446594DU;
        private const uint EncryptKey = 0x20534444U;

        private uint textureWidth;
        private uint textureHeight;
        private TextureFormat format;


        public int width => (int)textureWidth;

        public int height => (int)textureHeight;

        public TextureFormat textureFormat => textureFormat;

        public bool IsValid => (width > 0 && height >0);

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

        public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false)
        {
            var texture2d = new Texture2D(width, height, textureFormat, false, isLinearColor);
            using (var rawData = this.GeImageDataWithoutMipmap(fileBinary))
            {
                texture2d.LoadRawTextureData(rawData);
            }
            texture2d.Apply();

            return texture2d;
        }

        /// <summary>
        /// SignatureCheck
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