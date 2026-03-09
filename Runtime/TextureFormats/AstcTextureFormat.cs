using System.Collections;
using NUnit.Framework.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace UTJ.RuntimeCompressedTexturePacker.Format {

    /// <summary>
    /// Arm社より提供されているastc-encoder (astcenc) が書き出すASTCテクスチャツール
    /// </summary>
    public struct AstcTextureFormat: ITextureFormatFile
    {
        // ASTCのブロックの幅
        public byte block_x;
        // ASTCのブロックの高さ
        public byte block_y;
        // 基本的に使いません
        public byte block_z;

        // Texture自体の幅
        public uint dim_x;
        // Texture自体の高さ
        public uint dim_y;
        // 基本的に使いません
        public uint dim_z;

        /// <summary>
        /// テクスチャファイルの幅
        /// </summary>
        public int width => (int)dim_x;

        /// <summary>
        /// Textureファイルの高さ
        /// </summary>
        public int height => (int)dim_y;

        /// <summary>
        /// テクスチャフォーマット
        /// </summary>
        public TextureFormat textureFormat
        {
            get
            {
                TextureFormat format;
                GetTextureFormat(out format);
                return format;
            }        
        }

        /// <summary>
        /// データが正しいかを返します。
        /// </summary>
        public bool IsValid
        {
            get
            {
                TextureFormat format;
                return GetTextureFormat(out format);
            }
        }
        

        /// <summary>
        /// ASTC形式のHeaderのロード
        /// </summary>
        /// <param name="fileBinary">.astcファイルの内容</param>
        /// <returns>ファイルでロードできたかの可否</returns>
        public bool LoadHeader(NativeArray<byte> fileBinary)
        {
            // 先頭4Byte
            if ( !fileBinary.IsCreated || fileBinary.Length < 16 || 
                fileBinary[0] != 0x13 || fileBinary[1] != 0xAB || fileBinary[2] != 0xA1 || fileBinary[3] != 0x5C)
            {
                this.block_x = this.block_y = this.block_z = 0;
                this.dim_x = this.dim_y = this.dim_z = 0;
                return false;
            }
            // ASTCブロックサイズ
            this.block_x = fileBinary[4];
            this.block_y = fileBinary[5];
            this.block_z = fileBinary[6];

            // 画像サイズ 
            this.dim_x = (uint)(fileBinary[7] + (fileBinary[8] << 8) + (fileBinary[9] << 16));
            this.dim_y = (uint)(fileBinary[10] + (fileBinary[11] << 8) + (fileBinary[12] << 16));
            this.dim_z = (uint)(fileBinary[13] + (fileBinary[14] << 8) + (fileBinary[15] << 16));

            return true;
        }

        /// <summary>
        /// ファイル全体を渡して、画像の実データ部分だけを切り抜いて返します。
        /// </summary>
        /// <param name="fileBinary">ファイル全体のバイナリデータ</param>
        /// <returns>実データ部分</returns>

        public NativeArray<byte> GeImageData(NativeArray<byte> fileBinary)
        {
            return fileBinary.GetSubArray(16, fileBinary.Length - 16);
        }

        /// <summary>
        /// ASTCテクスチャファイルそのもののロードを行います
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap= false)
        {
            if( !this.LoadHeader(fileBinary))
            {
                return null;
            }
            if(!this.IsValid)
            {
                return null;
            }
            var tex = CreateFromHeader(isLinearColor);
            if(tex != null) {
                var rawData = this.GeImageData(fileBinary);
                tex.LoadRawTextureData( rawData);
                tex.Apply();
            }
            return tex;
        }


        /// <summary>
        /// Unityのテクスチャフォーマットを取得します
        /// </summary>
        /// <param name="format">UnityのTextureFormatを返します</param>
        /// <returns>対応するフォーマットがない場合 falseを返します</returns>
        private bool GetTextureFormat(out TextureFormat format)
        {
            if (this.block_x != this.block_y)
            {
                format = TextureFormat.ARGB32;
                return false;
            }
            switch (this.block_x)
            {
                case 4:
                    format = TextureFormat.ASTC_4x4;
                    return true;
                case 5:
                    format = TextureFormat.ASTC_5x5;
                    return true;
                case 6:
                    format = TextureFormat.ASTC_6x6;
                    return true;
                case 8:
                    format = TextureFormat.ASTC_8x8;
                    return true;
                case 10:
                    format = TextureFormat.ASTC_10x10;
                    return true;
                case 12:
                    format = TextureFormat.ASTC_12x12;
                    return true;
                default:
                    format = TextureFormat.ARGB32;
                    return false;
            }
        }
        // Texture作成
        private Texture2D CreateFromHeader(bool isLinearColor )
        {
            TextureFormat format;
            if(!GetTextureFormat(out format))
            {
                return null;
            }
            var texture = new Texture2D( (int)dim_x, (int)dim_y, format,false, isLinearColor);
            return texture;
        }

    }
}
