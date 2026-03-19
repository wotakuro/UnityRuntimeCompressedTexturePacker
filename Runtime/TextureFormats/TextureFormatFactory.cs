using Unity.Collections;
using UnityEngine;

namespace UTJ.RuntimeCompressedTexturePacker.Format
{
    /// <summary>
    /// TextureFile自体のフォーマット
    /// </summary>
    public static class TextureFormatFactory
    {
        public static ITextureFormatFile GetTextureFormat(NativeArray<byte> fileBinary)
        {
            if (AstcTextureFormat.SignatureValid(fileBinary))
            {
                return new AstcTextureFormat();
            }else if (KtxV1TextureFormat.SignatureValid(fileBinary))
            {
                return new KtxV1TextureFormat();
            }
            else if (DdsTextureFormat.SignatureValid(fileBinary))
            {
                return new DdsTextureFormat();
            }
            return new NullTextureFormat();
        }
    }
}