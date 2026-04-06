using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace UTJ.RuntimeCompressedTexturePacker.Format
{
    /// <summary>
    /// TextureFileフォーマット関連のUtility
    /// </summary>
    public static class TextureFileFormatUtility
    {
        /// <summary>
        /// 独自のTextureFomat（暗号化等）を対応するためのDelgateを用意
        /// </summary>
        /// <param name="fileBinary">ファイルのバイナリデータが入ります</param>
        /// <returns>適切なファイルフォーマットを返します</returns>
        public delegate ITextureFileFormat AppendFormatDetelctFuncction(NativeArray<byte> fileBinary);

        /// <summary>
        /// 独自のFileFormatを追加する必要がある場合は設定してください
        /// </summary>
        public static AppendFormatDetelctFuncction appendFormatDetelctFuncction;

        /// <summary>
        /// Binaryデータから、ファイルフォーマットを判定して返します
        /// </summary>
        /// <param name="fileBinary">ファイルバイナリ全体</param>
        /// <returns>データに対応したファイルフォーマットオブジェクトを返します</returns>
        public static ITextureFileFormat GetTextureFileFormatObject(NativeArray<byte> fileBinary)
        {
            if (appendFormatDetelctFuncction != null)
            {
                var format = appendFormatDetelctFuncction(fileBinary);
                if (!(format is NullTextureFile))
                {
                    return format;
                }
            }
            if (AstcTextureFile.SignatureValid(fileBinary))
            {
                return new AstcTextureFile();
            }
            else if (KtxV1TextureFile.SignatureValid(fileBinary))
            {
                return new KtxV1TextureFile();
            }
            else if (DdsTextureFile.SignatureValid(fileBinary))
            {
                return new DdsTextureFile();
            }
            return new NullTextureFile();
        }

        /// <summary>
        /// 指定されたTextureFormatがサポートされているかを返します
        /// </summary>
        /// <param name="textureFormat">テクスチャフォーマットの指定</param>
        /// <returns>対応している場合Trueを返します</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSupportedTextureFormat(TextureFormat textureFormat)
        {
#if UNITY_EDITOR
            // EditorはDecompressorがあるので、問題ない
            return true;
#else
            return SystemInfo.SupportsTextureFormat(textureFormat);

#endif
        }

        public static void GetBlockInfo(TextureFormat textureFormat,out int blockX,out int blockY,out int blockByteSize)
        {

            blockX = blockY = 0;
            blockByteSize = 0;
            switch (textureFormat)
            {
                case TextureFormat.ASTC_4x4:                   
                    blockX = blockY = 4;
                    blockByteSize = 16;
                    break;
                case TextureFormat.ASTC_5x5:
                    blockX = blockY = 5;
                    blockByteSize = 16;
                    break;
                case TextureFormat.ASTC_6x6:
                    blockX = blockY = 6;
                    blockByteSize = 16;
                    break;
                case TextureFormat.ASTC_8x8:
                    blockX = blockY = 8;
                    blockByteSize = 16;
                    break;
                case TextureFormat.ASTC_10x10:
                    blockX = blockY = 10;
                    blockByteSize = 16;
                    break;
                case TextureFormat.ASTC_12x12:
                    blockX = blockY = 12;
                    blockByteSize = 16;
                    break;
                // DXT1 4x4 64Bit (8Byte)
                case TextureFormat.DXT1:
                    blockX = blockY = 4;
                    blockByteSize = 8;
                    break;
                // DXT5 4x4 128Bit (16Byte)
                case TextureFormat.DXT5:
                    blockX = blockY = 4;
                    blockByteSize = 16;
                    break;
                // BC7 4x4 128Bit (16Byte)
                case TextureFormat.BC7:
                    blockX = blockY = 4;
                    blockByteSize = 16;
                    break;
                // ETC2 RGB 4x4 64bit (8Byte)
                case TextureFormat.ETC2_RGB:
                    blockX = blockY = 4;
                    blockByteSize = 8;
                    break;
                // ETC2 RGBA 4x4 128Bit (16Byte)
                case TextureFormat.ETC2_RGBA8:
                    blockX = blockY = 4;
                    blockByteSize = 16;
                    break;
                // ETC2 RGBA1 4x4 64Bit(8Byte)
                case TextureFormat.ETC2_RGBA1:
                    blockX = blockY = 4;
                    blockByteSize = 8;
                    break;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textureFormatFile"></param>
        /// <param name="fileBinary"></param>
        /// <param name="isLinearColor"></param>
        /// <returns></returns>
        public static Texture2D CreateTextureWithoutMipmap(in ITextureFileFormat textureFormatFile,NativeArray<byte> fileBinary,bool isLinearColor)
        {
            if (!textureFormatFile.LoadHeader(fileBinary))
            {
                return null;
            }
            if (!textureFormatFile.IsValid)
            {
                return null;
            }
            if(!IsSupportedTextureFormat(textureFormatFile.textureFormat))
            {
                return null;
            }
            int width = textureFormatFile.width;
            int height = textureFormatFile.height;
            if (ShouldMultiple4Size(textureFormatFile.textureFormat))
            {
                ExpandSizeToMultiple4Size(ref width, ref height);
            }

            var tex = new Texture2D( width, height, textureFormatFile.textureFormat, false, isLinearColor);
            if (tex != null)
            {
                var rawData = textureFormatFile.GeImageDataWithoutMipmap(fileBinary);
                tex.LoadRawTextureData(rawData);
                tex.Apply();
            }
            return tex;
        }
        
        /// <summary>
        /// 幅及び高さを4の倍数に拡張します
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ExpandSizeToMultiple4Size(ref int width, ref int height)
        {
            width = ((width + 3) / 4) * 4;
            height = ((height + 3) / 4) * 4;
        }

        /// <summary>
        /// 4の倍数である必要があるフォーマットか？
        /// </summary>
        /// <param name="format">テクスチャフォーマット</param>
        /// <returns>4の倍数であるひつようならTrue </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldMultiple4Size(TextureFormat format)
        {
            switch (format)
            {
                // BC
                case TextureFormat.BC4:                
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:
                // DXT
                case TextureFormat.DXT1:
                case TextureFormat.DXT5:
                // ETC
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGB:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ETC2_RGBA1:
                    return true;

            }
            return false;
        }


        /// <summary>
        /// Textureのフォーマットと幅、高さから必要なデータサイズを返します
        /// </summary>
        /// <param name="format">テクスチャフォーマット</param>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <returns></returns>
        public static int GetDataSize(TextureFormat format, int width, int height)
        {
            switch (format)
            {
                // ASTC block,
                case TextureFormat.ASTC_4x4:
                    return GetTextureDataSize(4, 4, 16, width, height);
                case TextureFormat.ASTC_5x5:
                    return GetTextureDataSize(5, 5, 16, width, height);
                case TextureFormat.ASTC_6x6:
                    return GetTextureDataSize(6, 6, 16, width, height);
                case TextureFormat.ASTC_8x8:
                    return GetTextureDataSize(8, 8, 16, width, height);
                case TextureFormat.ASTC_10x10:
                    return GetTextureDataSize(10, 10, 16, width, height);
                case TextureFormat.ASTC_12x12:
                    return GetTextureDataSize(12, 12, 16, width, height);
                // DXT1 4x4 64Bit (8Byte)
                case TextureFormat.DXT1:
                    return GetTextureDataSize(4, 4, 8, width, height);
                // DXT5 4x4 128Bit (16Byte)
                case TextureFormat.DXT5:
                    return GetTextureDataSize(4, 4, 16, width, height);
                // BC7 4x4 128Bit (16Byte)
                case TextureFormat.BC7:
                    return GetTextureDataSize(4, 4, 16, width, height);
                // ETC2 RGB 4x4 64bit (8Byte)
                case TextureFormat.ETC2_RGB:
                    return GetTextureDataSize(4, 4, 8, width, height);
                // ETC2 RGBA 4x4 128Bit (16Byte)
                case TextureFormat.ETC2_RGBA8:
                    return GetTextureDataSize(4, 4, 16, width, height);
                // ETC2 RGBA1 4x4 64Bit(8Byte)
                case TextureFormat.ETC2_RGBA1:
                    return GetTextureDataSize(4, 4, 8, width, height);
            }
            return 0;
        }

        // ASTCのブロックサイズ、Textureのサイズを受け取り、テクスチャ自体のデータサイズを返します
        private static int GetTextureDataSize(int block_x, int block_y, int blockByte, int width, int height)
        {
            int blockXnum = (width + block_x - 1) / block_x;
            int blockYnum = (height + block_y - 1) / block_y;
            return blockXnum * blockYnum * blockByte;
        }
    }
}