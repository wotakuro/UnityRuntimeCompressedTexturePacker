using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using System;
using UTJ.RuntimeCompressedTexturePacker.Packing;

namespace UTJ.RuntimeCompressedTexturePacker
{

    /// <summary>
    /// 圧縮されたTextureフォーマットをパッキングします
    /// （現在はASTCのみサポート）
    /// </summary>
    public class CompressedTexturePacker : IDisposable
    {
        // Texture format
        private TextureFormat textureFormat;
        // Texture size
        private int textureWidth;
        private int textureHeight;

        // texture実データのバッファー
        private NativeArray<byte> textureLowData;
        // 作成されたRect
        private Dictionary<string , RectInt> rects = new Dictionary<string, RectInt>();
        // 空きスペース矩形の解決をするアルゴリズム
        private IRectResolveAlgorithm rectResolveAlgorithm;

        // astc BlockX,BlockY
        private int blockX;
        private int blockY;

        /// <summary>
        /// マージンをいれるピクセル
        /// </summary>
        public int marginPixel { get; set; } = 1;

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
                new UTJ.RuntimeCompressedTexturePacker.Packing.MaximalRectanglesPacking());
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">Packing Textureの幅</param>
        /// <param name="height">Packing Textureの幅</param>
        /// <param name="textureFormat">パッキングされるTextureのフォーマット</param>
        /// <param name="resolveAlgorithm">生成に利用するアルゴリズム</param>
        public CompressedTexturePacker(int width , int height, TextureFormat textureFormat, bool isLinearColor, IRectResolveAlgorithm resolveAlgorithm)
        {
            Initialize(width,height, textureFormat, isLinearColor,resolveAlgorithm);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="srcTextureLowData"></param>
        /// <returns></returns>
        public Rect AppendTextureData(string name, int width, int height, NativeArray<byte> srcTextureLowData)
        {
            this.InitLowTextureDataBufferIfNeeded();
            RectInt rectInt = new RectInt();
            if (!ValidateDataLength(width, height, srcTextureLowData.Length))
            {
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
            this.rects.Add(name, rectInt);
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
                case TextureFormat.ASTC_4x4:
                    return GetAstcDataSize(4, 4, width, height);
                case TextureFormat.ASTC_5x5:
                    return GetAstcDataSize(5, 5, width, height);
                case TextureFormat.ASTC_6x6:
                    return GetAstcDataSize(6, 6, width, height);
                case TextureFormat.ASTC_8x8:
                    return GetAstcDataSize(8, 8, width, height);
                case TextureFormat.ASTC_10x10:
                    return GetAstcDataSize(10, 10, width, height);
                case TextureFormat.ASTC_12x12:
                    return GetAstcDataSize(12, 12, width, height);
            }
            return 0;
        }


        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="width">Packing Textureの幅</param>
        /// <param name="height">Packing Textureの幅</param>
        /// <param name="format">パッキングされるTextureのフォーマット</param>
        /// <param name="isLinearColor">リニアカラー</param>
        /// <param name="resolveAlgorithm">生成に利用するアルゴリズム</param>
        private void Initialize( int width, int height, TextureFormat format, bool isLinearColor, IRectResolveAlgorithm resolveAlgorithm)
        {
            this.textureFormat = format;
            this.textureWidth = width;
            this.textureHeight = height;

            switch (textureFormat)
            {
                case TextureFormat.ASTC_4x4:
                    this.blockX = this.blockY = 4;
                    break;
                case TextureFormat.ASTC_5x5:
                    this.blockX = this.blockY = 5;
                    break;
                case TextureFormat.ASTC_6x6:
                    this.blockX = this.blockY = 6;
                    break;
                case TextureFormat.ASTC_8x8:
                    this.blockX = this.blockY = 8;
                    break;
                case TextureFormat.ASTC_10x10:
                    this.blockX = this.blockY = 10;
                    break;
                case TextureFormat.ASTC_12x12:
                    this.blockX = this.blockY = 12;
                    break;
            }
            this.rectResolveAlgorithm = resolveAlgorithm;
            this.rectResolveAlgorithm.Initialize(width, height);
            this.texture2D = new Texture2D(this.textureWidth, this.textureHeight, this.textureFormat, false, isLinearColor);
        }




        // 必要ならば、TextureのLowDataを確保し、アルファ０で塗りつぶします
        private void InitLowTextureDataBufferIfNeeded()
        {
            if (!this.textureLowData.IsCreated)
            {
                this.textureLowData = new NativeArray<byte>(GetDataSize(this.textureFormat, this.textureWidth, this.textureHeight), Allocator.Persistent);
                switch (this.textureFormat) {
                    case TextureFormat.ASTC_4x4:
                    case TextureFormat.ASTC_5x5:
                    case TextureFormat.ASTC_6x6:
                    case TextureFormat.ASTC_8x8:
                    case TextureFormat.ASTC_10x10:
                    case TextureFormat.ASTC_12x12:
                        this.AstcAlphaClearBuffer();
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
                ulong* dstPtr = (ulong*)this.textureLowData.GetUnsafePtr();
                ulong* srcPtr = (ulong*)srcTexLowData.GetUnsafePtr();
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
                    dstPtr += ( (allBlockXNum - writeBlockXNum) * 2);
                }
            }
        }


        // Textureの幅とサイズ、データ自体の長さを元にValidationを行います
        private bool ValidateDataLength(int width,int height,int length)
        {
            int expectedLength = GetDataSize(this.textureFormat, width, height);
            return (expectedLength == length);
        }

        // ASTCのブロックサイズ、Textureのサイズを受け取り、テクスチャ自体のデータサイズを返します
        private static int GetAstcDataSize(int block_x, int block_y, int width, int height)
        {
            int blockXnum = (width + block_x - 1) / block_x;
            int blockYnum = (height + block_y - 1) / block_y;
            return blockXnum * blockYnum * 16;
        }

        // 全てのPixelをASTCでクリアします
        private void AstcAlphaClearBuffer()
        {
            if (!textureLowData.IsCreated)
            {
                return;
            }
            unsafe
            {
                ulong* clearData = stackalloc ulong[2];
                SetCleardata((byte*)clearData);
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

        // クリアになるような ASTCブロックのデータを作成
        private unsafe void SetCleardata(byte* setupData)
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

    }
}