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

        /// <summary>
        /// 起動時に、Delegate処理を登録します
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        static void RegisterFileFormats()
        {
            TextureFileFormatUtility.appendFormatDetelctFuncction = GetAppTextureFileFormat;
        }

        /// <summary>
        /// テクスチャファイル形式選択のDelegate処理部分
        /// </summary>
        /// <param name="fileBinary">ファイルの中身</param>
        /// <returns>適切なファイルフォーマットを返します</returns>
        private static ITextureFileFormat GetAppTextureFileFormat(NativeArray<byte> fileBinary)
        {
            // ファイルの先頭を見て、今回作成した独自ファイル形式なら、それを返します
            if (EncryptedTextureFileFormat.SignatureValid(fileBinary)) {
                return new EncryptedTextureFileFormat();
            }
            //　独自の処理の中では見つからなかったので、システムデフォルトに任せます
            return new NullTextureFile();
        }
    }
}