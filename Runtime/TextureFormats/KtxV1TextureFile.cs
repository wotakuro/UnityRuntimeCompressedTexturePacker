using System;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using NUnit.Framework.Interfaces;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Analytics;

namespace UTJ.RuntimeCompressedTexturePacker.Format {

    /// <summary>
    /// Arm社より提供されているastc-encoder (astcenc) が書き出すASTCテクスチャツール
    /// https://github.com/ARM-software/astc-encoder
    /// </summary>
    public struct KtxV1TextureFile : ITextureFileFormat
    {

        // エンディアン　0x04030201ならリトルエンディアン
        private uint endianness;
        // 圧縮テクスチャなら0
        private uint glType;
        // 圧縮テクスチャデータなら1
        private uint glTypeSize;
        // 圧縮テクスチャなら 0
        private uint glFormat;
        // GLでの値 GL_COMPRESSED_RGB8_ETC2等
        private uint glInternalFormat;
        // GLでのピクセルの扱い GL_RGB等
        private uint glBaseInternalFormat;
        // ピクセル幅
        private uint pixelWidth;
        // ピクセル高さ
        private uint pixelHeight;
        // ピクセル深度
        private uint pixelDepth;
        // Texture配列数
        private uint numberOfArrayElements;
        // Face数
        private uint numberOfFaces;
        // みっぷマップ数
        private uint numberOfMipmapLevels;
        // 実データへのオフセット
        private uint bytesOfKeyValueData;

        //GLの値
        #region GL_FORMAT_VALUES
        /// <summary>
        /// glInternalFormatの値
        /// </summary>
        private enum KtxGlIntegerFormat : uint
        {
            // --- ETC ---
            // アルファなし ETC2
            GL_COMPRESSED_RGB8_ETC2 = 0x9274,
            // sRGBアルファなし ETC2
            GL_COMPRESSED_SRGB8_ETC2 = 0x9275,
            // アルファ付き ETC2
            GL_COMPRESSED_RGBA8_ETC2_EAC = 0x9278,
            // sRGB アルファ付き ETC2 
            GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC = 0x9279,
            // 1bitアルファETC2
            GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276,
            // sRGB 1bitアルファETC2 
            GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277,
            // ETC1圧縮
            GL_ETC1_RGB8_OES = 0x8D64,

            // --- ASTC Linear (リニア空間 / HDR含む) ---
            COMPRESSED_RGBA_ASTC_4x4_KHR = 0x93B0,
            COMPRESSED_RGBA_ASTC_5x5_KHR = 0x93B2,
            COMPRESSED_RGBA_ASTC_6x6_KHR = 0x93B4,
            COMPRESSED_RGBA_ASTC_8x8_KHR = 0x93B7,
            COMPRESSED_RGBA_ASTC_10x10_KHR = 0x93BB,
            COMPRESSED_RGBA_ASTC_12x12_KHR = 0x93BD,

            // --- ASTC sRGB (カラーテクスチャ用) ---
            COMPRESSED_SRGB8_ALPHA8_ASTC_4x4_KHR = 0x93D0,
            COMPRESSED_SRGB8_ALPHA8_ASTC_5x5_KHR = 0x93D2,
            COMPRESSED_SRGB8_ALPHA8_ASTC_6x6_KHR = 0x93D4,
            COMPRESSED_SRGB8_ALPHA8_ASTC_8x8_KHR = 0x93D7,
            COMPRESSED_SRGB8_ALPHA8_ASTC_10x10_KHR = 0x93DB,
            COMPRESSED_SRGB8_ALPHA8_ASTC_12x12_KHR = 0x93DD,

            // --- BC (DXT / BPTC / RGTC) ---
            // BC1
            COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1,
            // BC2
            COMPRESSED_RGBA_S3TC_DXT3_EXT = 0x83F2,
            // BC3
            COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3,
            // BC4
            COMPRESSED_RED_RGTC1 = 0x8DBB,
            // BC5
            COMPRESSED_RG_RGTC2 = 0x8DBD,
            // BC6H
            COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT = 0x8E8F,
            // BC7
            COMPRESSED_RGBA_BPTC_UNORM = 0x8E8C  
        };

        /// <summary>
        /// glBaseInternalFormatの値
        /// </summary>
        private enum KtxBaseInternalFormat : uint
        {
            // Redのみ
            RED = 0x1903,
            // Alphaのみ
            GL_ALPHA = 0x1906,
            // RGB
            RGB = 0x1907,
            // RGBA
            RGBA = 0x1908,
            // RG
            RG = 0x8227,
            // 輝度
            LUMINANCE = 0x1909,
            // 輝度＋アルファ
            LUMINANCE_ALPHA = 0x190A,
        }
        #endregion

        public int width => (int)pixelWidth;

        public int height => (int)pixelHeight;

        public TextureFormat textureFormat
        {
            get
            {
                switch (glInternalFormat)
                {
                    // ETC 
                    case (uint)KtxGlIntegerFormat.GL_COMPRESSED_RGB8_ETC2:
                    case (uint)KtxGlIntegerFormat.GL_COMPRESSED_SRGB8_ETC2:
                        return TextureFormat.ETC2_RGB;
                    case (uint)KtxGlIntegerFormat.GL_COMPRESSED_RGBA8_ETC2_EAC:
                    case (uint)KtxGlIntegerFormat.GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC:
                        return TextureFormat.ETC2_RGBA8;
                    case (uint)KtxGlIntegerFormat.GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2:
                    case (uint)KtxGlIntegerFormat.GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2:
                        return TextureFormat.ETC2_RGBA1;
                    case (uint)KtxGlIntegerFormat.GL_ETC1_RGB8_OES:
                        return TextureFormat.ETC_RGB4;
                    // astc
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_ASTC_4x4_KHR:
                    case (uint)KtxGlIntegerFormat.COMPRESSED_SRGB8_ALPHA8_ASTC_4x4_KHR:
                        return TextureFormat.ASTC_4x4;
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_ASTC_5x5_KHR:
                    case (uint)KtxGlIntegerFormat.COMPRESSED_SRGB8_ALPHA8_ASTC_5x5_KHR:
                        return TextureFormat.ASTC_5x5;
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_ASTC_6x6_KHR:
                    case (uint)KtxGlIntegerFormat.COMPRESSED_SRGB8_ALPHA8_ASTC_6x6_KHR:
                        return TextureFormat.ASTC_6x6;
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_ASTC_8x8_KHR:
                    case (uint)KtxGlIntegerFormat.COMPRESSED_SRGB8_ALPHA8_ASTC_8x8_KHR:
                        return TextureFormat.ASTC_8x8;
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_ASTC_10x10_KHR:
                    case (uint)KtxGlIntegerFormat.COMPRESSED_SRGB8_ALPHA8_ASTC_10x10_KHR:
                        return TextureFormat.ASTC_10x10;
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_ASTC_12x12_KHR:
                    case (uint)KtxGlIntegerFormat.COMPRESSED_SRGB8_ALPHA8_ASTC_12x12_KHR:
                        return TextureFormat.ASTC_12x12;

                    // --- BC (DXT / BPTC / RGTC) ---
                    // BC1
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_S3TC_DXT1_EXT:
                        return TextureFormat.DXT1;
                    // BC3(DXT5)
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_S3TC_DXT5_EXT:
                        return TextureFormat.DXT5;
                    // BC4
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RED_RGTC1:
                        return TextureFormat.BC4;
                    // BC5
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RG_RGTC2:
                        return TextureFormat.BC5;
                    // BC6H
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT:
                        return TextureFormat.BC6H;
                    // BC7
                    case (uint)KtxGlIntegerFormat.COMPRESSED_RGBA_BPTC_UNORM:
                        return TextureFormat.BC7;
                }
                return TextureFormat.ARGB32;
            }
        }

        public bool IsValid
        {
            get {
                if (this.textureFormat == TextureFormat.ARGB32)
                {
                    return false;
                }
                return true;
            }
        }

        public NativeArray<byte> GeImageDataWithoutMipmap(NativeArray<byte> fileBinary)
        {
            int head = 64 + (int)this.bytesOfKeyValueData;
            uint size = BytesToOtherTypesUtility.ReadUintFast(fileBinary, head);
            return fileBinary.GetSubArray(head+4, (int)size);
        }

        public bool LoadHeader(NativeArray<byte> fileBinary)
        {
            if (!SignatureValid(fileBinary))
            {
                this.endianness = 0;
                this.pixelWidth = 0;
                this.pixelHeight = 0;
                return false;
            }
            this.endianness = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 12);
            this.glType = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 16);
            this.glTypeSize = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 20);
            this.glFormat = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 24);
            this.glInternalFormat = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 28);
            this.glBaseInternalFormat = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 32);

            this.pixelWidth = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 36);
            this.pixelHeight = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 40);
            this.pixelDepth = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 44);
            this.numberOfArrayElements = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 48);
            this.numberOfFaces = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 52);
            this.numberOfMipmapLevels = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 56);
            this.bytesOfKeyValueData = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 60);

            bool shouldSwap = ((this.endianness == 0x04030201U) != BytesToOtherTypesUtility.IsLittleEndianRuntime());
            if (shouldSwap)
            {
                this.glType = BytesToOtherTypesUtility.SwapUintEndian(this.glType);
                this.glTypeSize = BytesToOtherTypesUtility.SwapUintEndian(this.glTypeSize);
                this.glFormat = BytesToOtherTypesUtility.SwapUintEndian(this.glFormat);
                this.glInternalFormat = BytesToOtherTypesUtility.SwapUintEndian(this.glInternalFormat);
                this.glBaseInternalFormat = BytesToOtherTypesUtility.SwapUintEndian(this.glBaseInternalFormat);

                this.pixelWidth = BytesToOtherTypesUtility.SwapUintEndian(this.pixelWidth);
                this.pixelHeight = BytesToOtherTypesUtility.SwapUintEndian(this.pixelHeight);
                this.pixelDepth = BytesToOtherTypesUtility.SwapUintEndian(this.pixelDepth);
                this.numberOfArrayElements = BytesToOtherTypesUtility.SwapUintEndian(this.numberOfArrayElements);
                this.numberOfFaces = BytesToOtherTypesUtility.SwapUintEndian(this.numberOfFaces);
                this.numberOfMipmapLevels = BytesToOtherTypesUtility.SwapUintEndian(this.numberOfMipmapLevels);
                this.bytesOfKeyValueData = BytesToOtherTypesUtility.SwapUintEndian(this.bytesOfKeyValueData);

            }


            return true;
        }

        public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false)
        {
            return TextureFileFormatUtility.CreateTextureWithoutMipmap(this, fileBinary, isLinearColor);
        }

        /// <summary>
        /// ファイル形式のチェックを行います
        /// </summary>
        /// <param name="fileBinary">ファイルの中身</param>
        /// <returns>先頭数Byteを読み込んで、対象のフォーマットであるかを確認します</returns>
        public static bool SignatureValid(NativeArray<byte> fileBinary)
        {
            if(!fileBinary.IsCreated || fileBinary.Length < 64)
            {
                return false;
            }
            if (fileBinary[0] == 0xAB && fileBinary[1] == 0x4B && fileBinary[2] == 0x54 && fileBinary[3] == 0x58 &&
                fileBinary[4] == 0x20 && fileBinary[5] == 0x31 && fileBinary[6] == 0x31 && fileBinary[7] == 0xBB &&
                fileBinary[8] == 0x0D && fileBinary[9] == 0x0A && fileBinary[10] == 0x1A && fileBinary[11] == 0x0A)
            {
                return true;
            }
            return false;
        }

    }
}
