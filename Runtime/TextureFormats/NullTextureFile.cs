using Unity.Collections;
using UnityEngine;


namespace UTJ.RuntimeCompressedTexturePacker.Format {
    /// <summary>
    /// 適切なTextureFormatが無かった時
    /// </summary>
    public unsafe struct NullTextureFile : ITextureFileFormat
    {
        public int width => 0;

        public int height => 0;

        public TextureFormat textureFormat => TextureFormat.RGBA32;

        public bool IsValid => false;

        public NativeArray<byte> GeImageDataWithoutMipmap(NativeArray<byte> fileBinary)
        {
            return fileBinary;
        }

        public bool LoadHeader(NativeArray<byte> fileBinary)
        {
            return false;
        }

        public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false)
        {
            return null;
        }
    }
}
