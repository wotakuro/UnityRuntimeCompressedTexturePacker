using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using System;
using UTJ.RuntimeCompressedTexturePacker.Packing;
using System.Runtime.CompilerServices;
using UTJ.RuntimeCompressedTexturePacker.Format;

namespace UTJ.RuntimeCompressedTexturePacker
{

    /// <summary>
    /// 圧縮されたTextureフォーマットをパッキングします
    /// （現在はASTCのみサポート）
    /// </summary>
    public class CompressedTexturePacker : IDisposable
    {
        // Texture format
        public TextureFormat textureFormat {  get; private set; }
        // Texture size
        private int textureWidth;
        private int textureHeight;


        // texture実データのバッファー
        private NativeArray<byte> textureLowData;
        // 空きスペース矩形の解決をするアルゴリズム
        private IRectResolveAlgorithm rectResolveAlgorithm;

        // Compressed Texture BlockX,BlockY and size/Block
        private int blockX;
        private int blockY;
        private int blockByteSize;

        /// <summary>
        /// マージンをいれるピクセル
        /// </summary>
        public int marginPixel { get;private set; } = 1;

        /// <summary>
        /// 内部で生成したTexture
        /// </summary>
        public Texture2D texture2D { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// sRGBで、生成アルゴリズムはMaximalRectを利用したパッキングを行います
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="textureFormat">テクスチャフォーマット</param>
        public CompressedTexturePacker(int width, int height, TextureFormat textureFormat)
        {
            Initialize( width, height, textureFormat, false,
                new UTJ.RuntimeCompressedTexturePacker.Packing.MaximalRectanglesPacking(),1);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">Packing Textureの幅</param>
        /// <param name="height">Packing Textureの幅</param>
        /// <param name="textureFormat">パッキングされるTextureのフォーマット</param>
        /// <param name="resolveAlgorithm">生成に利用するアルゴリズム</param>
        /// <param name="margin">マージンピクセル数</param>
        public CompressedTexturePacker(int width , int height, TextureFormat textureFormat, bool isLinearColor, IRectResolveAlgorithm resolveAlgorithm,int margin)
        {
            Initialize(width,height, textureFormat, isLinearColor,resolveAlgorithm , margin);
        }

        /// <summary>
        /// テクスチャーのデータを挿入できるかを返します
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <returns></returns>
        public bool CanAppendTextureData(int width,int height)
        {
            int withMarginWidth = ((width + blockX - 1 + marginPixel) / blockX) * blockX;
            int withMarginHeight = ((height + blockY - 1 + marginPixel) / blockY) * blockY;
            return this.rectResolveAlgorithm.CanInsert(withMarginWidth, withMarginHeight);
        }

        /// <summary>
        /// テクスチャーのデータを実際に行います
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="srcTextureLowData">テクスチャデータ</param>
        /// <returns>見つかったRectを返します</returns>
        public Rect AppendTextureData(int width, int height, NativeArray<byte> srcTextureLowData)
        {
            this.InitLowTextureDataBufferIfNeeded();
            RectInt rectInt = new RectInt();
            if (!ValidateDataLength(width, height, srcTextureLowData.Length))
            {
                Debug.LogWarning("data length is not valid " + srcTextureLowData.Length);
                return Rect.zero;
            }
            int bodyWidth = ((width + blockX - 1 ) / blockX) * blockX;
            int bodyHeight = ((height + blockY - 1 ) / blockY) * blockY;
            int withMarginWidth = ((width + blockX - 1 + marginPixel) / blockX) * blockX;
            int withMarginHeight = ((height + blockY - 1 + marginPixel) / blockY) * blockY;

            if (!this.rectResolveAlgorithm.Insert(withMarginWidth, withMarginHeight, out rectInt))
            {
                return Rect.zero;
            }
            rectInt.width = bodyWidth;
            rectInt.height = bodyHeight;
            WriteTextureData(rectInt, srcTextureLowData);
            // オリジナルの値に戻します
            rectInt.width = width;
            rectInt.height = height;
            return ConvertRectF(rectInt);
        }

        /// <summary>
        /// 書き込んだTextureのローデータを実際のTextureに適用します
        /// </summary>
        public void ApplyToTexture()
        {
            texture2D.LoadRawTextureData(this.textureLowData);
            texture2D.Apply();
        }

        /// <summary>
        /// TextureのLowデータを破棄します。破棄すると、以後の書き換え追加は行えません。
        /// まっさらな状態からスタートします。
        /// </summary>
        public void ReleaseTextureLowData()
        {
            if (this.textureLowData.IsCreated)
            {
                this.textureLowData.Dispose();
                this.rectResolveAlgorithm.Initialize(this.textureWidth, this.textureHeight);
            }
        }

        /// <summary>
        /// 特定区域をクリアします
        /// </summary>
        /// <param name="rect">Rectの指定</param>
        public unsafe void RemoveRect(in RectInt rect)
        {
            this.rectResolveAlgorithm.Remove(rect);
            switch (this.textureFormat)
            {
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_12x12:
                    this.ClearBufferRect(rect,SetupAstcCleardata);
                    break;
                case TextureFormat.DXT5:
                    this.ClearBufferRect(rect, SetupBC3UNormClearData);
                    break;
                case TextureFormat.BC7:
                    this.ClearBufferRect(rect, SetupBC7UNormClearData);
                    break;
                case TextureFormat.ETC2_RGBA8:
                    this.ClearBufferRect(rect, SetupEtc2RGBA8ClearData);
                    break;
                case TextureFormat.DXT1:
                    this.ClearBufferRect(rect, SetupBC1UNormClearData);
                    break;
                case TextureFormat.ETC2_RGBA1:
                    this.ClearBufferRect(rect, SetupEtc2RGBA1ClearData);
                    break;
                case TextureFormat.ETC2_RGB:
                    this.ClearBufferRect(rect, SetupEtc2RGBClearData);
                    break;
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// [Editor Only]TextureをDestroyImmediateする
        /// </summary>
        public void DestroyTextureImmediate()
        {

            if (texture2D)
            {
                UnityEngine.Object.DestroyImmediate(texture2D);
            }
            texture2D = null;
        }
        #endif

        /// <summary>
        /// Dispose処理
        /// </summary>
        public void Dispose()
        {
            ReleaseTextureLowData();
            if (texture2D)
            {
                UnityEngine.Object.Destroy(texture2D);
            }
            texture2D = null;
            if (rectResolveAlgorithm != null)
            {
                rectResolveAlgorithm.Dispose();
                rectResolveAlgorithm = null;
            }
        }



        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="width">Packing Textureの幅</param>
        /// <param name="height">Packing Textureの幅</param>
        /// <param name="format">パッキングされるTextureのフォーマット</param>
        /// <param name="isLinearColor">リニアカラー</param>
        /// <param name="resolveAlgorithm">生成に利用するアルゴリズム</param>
        /// <param name="margin">マージンピクセル数</param>
        private void Initialize( int width, int height, TextureFormat format, bool isLinearColor, IRectResolveAlgorithm resolveAlgorithm,
            int margin)
        {
            this.textureFormat = format;
            this.textureWidth = width;
            this.textureHeight = height;
            TextureFileFormatUtility.GetBlockInfo(format, out this.blockX, out this.blockY, out this.blockByteSize);
           

            this.rectResolveAlgorithm = resolveAlgorithm;
            this.rectResolveAlgorithm.Initialize(width, height);
            this.texture2D = new Texture2D(this.textureWidth, this.textureHeight, this.textureFormat, false, isLinearColor);
            this.marginPixel = margin;
        }




        // 必要ならば、TextureのLowDataを確保し、アルファ０で塗りつぶします
        private unsafe void InitLowTextureDataBufferIfNeeded()
        {
            if (!this.textureLowData.IsCreated)
            {
                this.textureLowData = new NativeArray<byte>(TextureFileFormatUtility.GetDataSize(this.textureFormat, this.textureWidth, this.textureHeight), Allocator.Persistent);
                switch (this.textureFormat) {
                    case TextureFormat.ASTC_4x4:
                    case TextureFormat.ASTC_5x5:
                    case TextureFormat.ASTC_6x6:
                    case TextureFormat.ASTC_8x8:
                    case TextureFormat.ASTC_10x10:
                    case TextureFormat.ASTC_12x12:
                        this.AllClearBufferFor16ByteBlock(SetupAstcCleardata);
                        break;
                    case TextureFormat.DXT5:
                        this.AllClearBufferFor16ByteBlock(SetupBC3UNormClearData);
                        break;
                    case TextureFormat.BC7:
                        this.AllClearBufferFor16ByteBlock(SetupBC7UNormClearData);
                        break;
                    case TextureFormat.ETC2_RGBA8:
                        this.AllClearBufferFor16ByteBlock(SetupEtc2RGBA8ClearData);
                        break;
                    case TextureFormat.DXT1:
                        this.AllClearBufferFor8ByteBlock(SetupBC1UNormClearData);
                        break;
                    case TextureFormat.ETC2_RGBA1:
                        this.AllClearBufferFor8ByteBlock(SetupEtc2RGBA1ClearData);
                        break;
                    case TextureFormat.ETC2_RGB:
                        this.AllClearBufferFor8ByteBlock(SetupEtc2RGBClearData);
                        break;
                }
            }
        }

        // RectIntをRectにｎ変えます
        private static Rect ConvertRectF(in RectInt rectInt)
        {
            var rect = new Rect();
            rect.x = rectInt.x;
            rect.y = rectInt.y;
            rect.width = rectInt.width;
            rect.height = rectInt.height;
            return rect;
        }

        // 指定した矩形に、Textureデータの書き込みをします
        private void WriteTextureData(RectInt rectInt, NativeArray<byte> srcTexLowData)
        {
            int writeBlockXStart = rectInt.x / this.blockX;
            int writeBlockYStart = rectInt.y / this.blockY;
            int writeBlockXNum = (rectInt.width + this.blockX -1) / this.blockX;
            int writeBlockYNum = (rectInt.height + this.blockY -1) / this.blockY;
            int allBlockXNum = (this.textureWidth + this.blockX -1) / this.blockX;

            unsafe
            {
                void* dstPtr = this.textureLowData.GetUnsafePtr();
                void* srcPtr = srcTexLowData.GetUnsafePtr();

                if (this.blockByteSize == 16)
                {
                    if (BytesToOtherTypesUtility.Is8ByteAlign(srcPtr))
                    {
                        Write16ByteBlock8ByteAlign(dstPtr, srcPtr,
                            allBlockXNum, writeBlockXStart, writeBlockYStart,
                            writeBlockXNum, writeBlockYNum);
                    }
                    else if (BytesToOtherTypesUtility.Is4ByteAlign(srcPtr))
                    {
                        Write16ByteBlock4ByteAlign(dstPtr, srcPtr,
                                allBlockXNum, writeBlockXStart, writeBlockYStart,
                                writeBlockXNum, writeBlockYNum);
                    }else
                    {
                        WriteBlockSlow(dstPtr, srcPtr, this.blockByteSize,
                                allBlockXNum, writeBlockXStart, writeBlockYStart,
                                writeBlockXNum, writeBlockYNum);
                    }
                }else if(this.blockByteSize == 8)
                {
                    if (BytesToOtherTypesUtility.Is8ByteAlign(srcPtr))
                    {
                        Write8ByteBlock8ByteAlign(dstPtr, srcPtr,
                            allBlockXNum, writeBlockXStart, writeBlockYStart,
                            writeBlockXNum, writeBlockYNum);
                    }
                    else if (BytesToOtherTypesUtility.Is4ByteAlign(srcPtr))
                    {
                        Write8ByteBlock4ByteAlign(dstPtr, srcPtr,
                                allBlockXNum, writeBlockXStart, writeBlockYStart,
                                writeBlockXNum, writeBlockYNum);
                    }
                    else
                    {
                        WriteBlockSlow(dstPtr, srcPtr, this.blockByteSize,
                                allBlockXNum, writeBlockXStart, writeBlockYStart,
                                writeBlockXNum, writeBlockYNum);
                    }

                }
            }
        }
        #region DATACOPY
        // 16Byteブロック、かつ8Byteアラインの状況で書き込みます
        private static unsafe void Write16ByteBlock8ByteAlign(void* dst,void *src,int allBlockXNum,
            int writeBlockXStart,int writeBlockYStart,
            int writeBlockXNum,int writeBlockYNum)
        {
            ulong* dstPtr = (ulong*)dst;
            ulong* srcPtr = (ulong*)src;
            int dstOffset = (((writeBlockYStart * allBlockXNum) + writeBlockXStart) * 2);

            dstPtr += dstOffset;

            for (int yIndex = 0; yIndex < writeBlockYNum; ++yIndex)
            {
                for (int xIndex = 0; xIndex < writeBlockXNum; ++xIndex)
                {
                    // write 128bit
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                }
                dstPtr += ((allBlockXNum - writeBlockXNum) * 2);
            }
        }

        // 16Byteブロック、かつ4Byteアラインの状況で書き込みます
        private static unsafe void Write16ByteBlock4ByteAlign(void* dst, void* src, int allBlockXNum,
            int writeBlockXStart, int writeBlockYStart,
            int writeBlockXNum, int writeBlockYNum)
        {
            uint* dstPtr = (uint*)dst;
            uint* srcPtr = (uint*)src;
            int dstOffset = (((writeBlockYStart * allBlockXNum) + writeBlockXStart) * 4);

            dstPtr += dstOffset;

            for (int yIndex = 0; yIndex < writeBlockYNum; ++yIndex)
            {
                for (int xIndex = 0; xIndex < writeBlockXNum; ++xIndex)
                {
                    // write 128bit
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                }
                dstPtr += ((allBlockXNum - writeBlockXNum) * 4);
            }
        }

        // 8Byteブロック、かつ8Byteアラインの状況で書き込みます
        private static unsafe void Write8ByteBlock8ByteAlign(void* dst, void* src, int allBlockXNum,
            int writeBlockXStart, int writeBlockYStart,
            int writeBlockXNum, int writeBlockYNum)
        {
            ulong* dstPtr = (ulong*)dst;
            ulong* srcPtr = (ulong*)src;
            int dstOffset = (((writeBlockYStart * allBlockXNum) + writeBlockXStart));

            dstPtr += dstOffset;

            for (int yIndex = 0; yIndex < writeBlockYNum; ++yIndex)
            {
                for (int xIndex = 0; xIndex < writeBlockXNum; ++xIndex)
                {
                    // write 64Bit
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                }
                dstPtr += ((allBlockXNum - writeBlockXNum));
            }
        }
        // 8Byteブロック、かつ5Byteアラインの状況で書き込みます
        private static unsafe void Write8ByteBlock4ByteAlign(void* dst, void* src, int allBlockXNum,
            int writeBlockXStart, int writeBlockYStart,
            int writeBlockXNum, int writeBlockYNum)
        {
            uint* dstPtr = (uint*)dst;
            uint* srcPtr = (uint*)src;
            int dstOffset = (((writeBlockYStart * allBlockXNum) + writeBlockXStart) * 2);

            dstPtr += dstOffset;

            for (int yIndex = 0; yIndex < writeBlockYNum; ++yIndex)
            {
                for (int xIndex = 0; xIndex < writeBlockXNum; ++xIndex)
                {
                    // write 64Bit
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                    *dstPtr = *srcPtr;
                    ++dstPtr; ++srcPtr;
                }
                dstPtr += ((allBlockXNum - writeBlockXNum)*2);
            }
        }

        // 1Byteずつ書き込みます。重いです
        private static unsafe void WriteBlockSlow(void* dst, void* src, int blockSize, int allBlockXNum,
            int writeBlockXStart, int writeBlockYStart,
            int writeBlockXNum, int writeBlockYNum)
        {
            byte* dstPtr = (byte*)dst;
            byte* srcPtr = (byte*)src;
            int dstOffset = (((writeBlockYStart * allBlockXNum) + writeBlockXStart) * blockSize);

            dstPtr += dstOffset;

            for (int yIndex = 0; yIndex < writeBlockYNum; ++yIndex)
            {
                for (int xIndex = 0; xIndex < writeBlockXNum; ++xIndex)
                {
                    // write 128bit
                    for (int i = 0; i < blockSize; ++i)
                    {
                        *dstPtr = *srcPtr;
                        ++dstPtr; ++srcPtr;
                    }
                }
                dstPtr += ((allBlockXNum - writeBlockXNum) * blockSize);
            }
        }
        #endregion DATACOPY


        // Textureの幅とサイズ、データ自体の長さを元にValidationを行います
        private bool ValidateDataLength(int width,int height,int length)
        {
            int expectedLength = TextureFileFormatUtility.GetDataSize(this.textureFormat, width, height);
            return (expectedLength == length);
        }

        private unsafe delegate void SetupClearData(byte* ptr);

        // 16Byte形式のブロック単位で全てのブロックをクリアします
        private void AllClearBufferFor16ByteBlock(SetupClearData setupClearDataFunc)
        {
            if (!textureLowData.IsCreated)
            {
                return;
            }
            unsafe
            {
                ulong* clearData = stackalloc ulong[2];
                setupClearDataFunc((byte*)clearData);
                ulong* dest = (ulong*) textureLowData.GetUnsafePtr();
                int length = textureLowData.Length / 16;
                for (int i=0;i<length; i++)
                {
                    dest[0] = clearData[0];
                    dest[1] = clearData[1];
                    dest += 2;
                }
            }
        }
        // 8Byte形式のブロック単位で全てのブロックをクリアします
        private void AllClearBufferFor8ByteBlock(SetupClearData setupClearDataFunc)
        {
            if (!textureLowData.IsCreated)
            {
                return;
            }
            unsafe
            {
                ulong* clearData = stackalloc ulong[2];
                setupClearDataFunc((byte*)clearData);
                ulong* dest = (ulong*)textureLowData.GetUnsafePtr();
                int length = textureLowData.Length / 8;
                for (int i = 0; i < length; i++)
                {
                    *dest = *clearData;
                    ++dest ;
                }
            }
        }


        // 指定区域をクリアします
        private void ClearBufferRect(in RectInt rectInt, SetupClearData setupClearDataFunc)
        {
            if (!textureLowData.IsCreated)
            {
                return;
            }
            unsafe
            {
                ulong* clearData = stackalloc ulong[2];
                setupClearDataFunc((byte*)clearData);

                int writeBlockXStart = rectInt.x / this.blockX;
                int writeBlockYStart = rectInt.y / this.blockY;
                int writeBlockXNum = (rectInt.width + this.blockX - 1) / this.blockX;
                int writeBlockYNum = (rectInt.height + this.blockY - 1) / this.blockY;
                int allBlockXNum = (this.textureWidth + this.blockX - 1) / this.blockX;

                int longSize = this.blockByteSize / 8;

                ulong* dstPtr = (ulong*)this.textureLowData.GetUnsafePtr();

                int dstOffset = (((writeBlockYStart * allBlockXNum) + writeBlockXStart) * longSize);

                dstPtr += dstOffset;

                if (this.blockByteSize == 16)
                {
                    for (int yIndex = 0; yIndex < writeBlockYNum; ++yIndex)
                    {
                        for (int xIndex = 0; xIndex < writeBlockXNum; ++xIndex)
                        {
                            // write 128bit
                            *dstPtr = clearData[0];
                            ++dstPtr;
                            *dstPtr = clearData[1];
                            ++dstPtr;
                        }
                        dstPtr += ((allBlockXNum - writeBlockXNum) * longSize);
                    }
                }
                else if (this.blockByteSize == 8)
                {
                    for (int yIndex = 0; yIndex < writeBlockYNum; ++yIndex)
                    {
                        for (int xIndex = 0; xIndex < writeBlockXNum; ++xIndex)
                        {
                            // write 648bit
                            *dstPtr = clearData[0];
                            ++dstPtr;
                        }
                        dstPtr += ((allBlockXNum - writeBlockXNum) * longSize);
                    }
                }
            }
        }

        // クリアになるような ASTCブロックのデータを作成
        private unsafe void SetupAstcCleardata(byte* setupData)
        {
            setupData[0] = 0xFC;
            setupData[1] = 0xFD;
            for (int i = 2; i <= 13; ++i)
            {
                setupData[i] = 0xFF;
            }
            setupData[14] = 0x00;
            setupData[15] = 0x00;
        }


        private unsafe void SetupEtc2RGBClearData(byte* setupData)
        {
            // 8byte
            for (int i = 0; i < 8; ++i)
            {
                setupData[i] = 0x00;
            }
            setupData[3] = 0x02;
            setupData[4] = 0xFF;
            setupData[5] = 0xFF;
        }
        private unsafe void SetupEtc2RGBA1ClearData(byte* setupData)
        {
            // 8byte
            for (int i = 0; i < 8; ++i)
            {
                setupData[i] = 0x00;
            }
            setupData[4] = 0xFF;
            setupData[5] = 0xFF;
        }

        private unsafe void SetupEtc2RGBA8ClearData(byte* setupData)
        {
            // 16byte
            for(int i = 0; i < 16; ++i)
            {
                setupData[i] = 0x00;
            }
            setupData[1] = 0x10;
            setupData[11] = 0x02;
        }

        private unsafe void SetupBC1UNormClearData(byte* setupData)
        {
            // 8byte
            for (int i = 0; i < 2; ++i)
            {
                setupData[i] = 0x00;
            }
            for (int i = 2; i < 8; ++i)
            {
                setupData[i] = 0xFF;
            }
        }

        private unsafe void SetupBC3UNormClearData(byte* setupData)
        {
            // 16byte
            setupData[0] = 0xFF;
            setupData[1] = 0x00;
            setupData[2] = 0x49;
            setupData[3] = 0x92;
            setupData[4] = 0x24;
            setupData[5] = 0x49;
            setupData[6] = 0x92;
            setupData[7] = 0x24;
            for (int i = 8; i < 16; ++i)
            {
                setupData[i] = 0x00;
            }
        }

        private unsafe void SetupBC7UNormClearData(byte* setupData)
        {
            // 16byte
            setupData[0] = 0x10;
            for (int i = 1; i < 16; ++i)
            {
                setupData[i] = 0x00;
            }
        }

    }
}