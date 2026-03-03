using System.Collections;
using Unity.Collections;
using UnityEngine;

namespace UTJ.RuntimeCompressedTexturePacker.Format {

    /// <summary>
    /// Arm社より提供されているastc-encoder (astcenc) が書き出すASTCテクスチャツール
    /// </summary>
    public struct AstcTextureFormat
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
        /// ASTC形式のHeaderのロード
        /// </summary>
        /// <param name="bytes">.astcファイルの内容</param>
        /// <returns>ファイルでロードできたかの可否</returns>
        public bool LoadHeader(NativeArray<byte> bytes)
        {
            // 先頭4Byte
            if ( !bytes.IsCreated || bytes.Length < 16 || 
                bytes[0] != 0x13 || bytes[1] != 0xAB || bytes[2] != 0xA1 || bytes[3] != 0x5C)
            {
                this.block_x = this.block_y = this.block_z = 0;
                this.dim_x = this.dim_y = this.dim_z = 0;
                return false;
            }
            // ASTCブロックサイズ
            this.block_x = bytes[4];
            this.block_y = bytes[5];
            this.block_z = bytes[6];

            // 画像サイズ 
            this.dim_x = (uint)(bytes[7] + (bytes[8] << 8) + (bytes[9] << 16));
            this.dim_y = (uint)(bytes[10] + (bytes[11] << 8) + (bytes[12] << 16));
            this.dim_z = (uint)(bytes[13] + (bytes[14] << 8) + (bytes[15] << 16));

            return true;
        }

        /// <summary>
        /// ASTCテクスチャファイルそのもののロードを行います
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public Texture2D LoadTexture(NativeArray<byte> bytes, bool isLinearColor = false)
        {
            this.LoadHeader(bytes);
            var tex = CreateFromHeader(isLinearColor);
            if(tex != null) {
                var rawData = bytes.GetSubArray(16, bytes.Length - 16);
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
        public bool GetTextureFormat(out TextureFormat format)
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
