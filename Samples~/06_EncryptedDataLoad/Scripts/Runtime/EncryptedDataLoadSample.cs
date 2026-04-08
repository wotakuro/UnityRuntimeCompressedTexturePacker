using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UTJ.RuntimeCompressedTexturePacker;
using UTJ.RuntimeCompressedTexturePacker.Format;

namespace UTJ.Sample
{
    /// <summary>
    /// 暗号化テクスチャ対応
    /// </summary>
    public class EncryptedDataLoadSample
    {

        [RuntimeInitializeOnLoadMethod]
        static void RegisterFileFormats()
        {
            TextureFileFormatUtility.appendFormatDetelctFuncction = GetAppTextureFileFormat;
        }
        private static ITextureFileFormat GetAppTextureFileFormat(NativeArray<byte> fileBinary)
        {
            if (EncryptedTextureFileFormat.SignatureValid(fileBinary)) {
                return new EncryptedTextureFileFormat();
            }
            return new NullTextureFile();
        }
    }
}