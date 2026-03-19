using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework.Interfaces;
using Unity.Collections;
using UnityEngine;
using UnityEngine.LightTransport;

namespace UTJ.RuntimeCompressedTexturePacker.Format {
    /// <summary>
    /// 適切なTextureFormatが無かった時
    /// </summary>
    public unsafe struct NullTextureFormat : ITextureFormatFile
    {
        public int width => throw new System.NotImplementedException();

        public int height => throw new System.NotImplementedException();

        public TextureFormat textureFormat => throw new System.NotImplementedException();

        public bool IsValid => throw new System.NotImplementedException();

        public NativeArray<byte> GeImageDataWithoutMipmap(NativeArray<byte> fileBinary)
        {
            throw new System.NotImplementedException();
        }

        public bool LoadHeader(NativeArray<byte> fileBinary)
        {
            throw new System.NotImplementedException();
        }

        public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
