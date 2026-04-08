using System.IO;
using UTJ.RuntimeCompressedTexturePacker;
using UnityEditor;
using Unity.Collections;
using UTJ.RuntimeCompressedTexturePacker.Format;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using System;

namespace UTJ.Sample
{
    /// <summary>
    /// 暗号化データ作成クラス
    /// </summary>
    public class GenerateEncryptData 
    {
        private const uint Signature = 0x5446594DU;
        private const uint EncryptKey = 0x20534444U;

        /* Just for reference (参照用)
        public struct EncryptDataheader
        {
            public uint Signature; // 0x5446594DU
            public uint width;
            public uint height;
            public uint textureFormat;
        }
        */

        /// <summary>
        /// ASTC / KTX / DDS ファイルを指定して、暗号化ファイルの作成
        /// </summary>
        [MenuItem("Tools/RuntimeCompressedTexturePacker/GenerateEncryptTexture/SelectTargetFile")]
        public static void SelectTargetFile()
        {
            var srcFile = EditorUtility.OpenFilePanel("Select TextureFile", "", "astc,ktx,dds");
            if (string.IsNullOrEmpty(srcFile))
            {
                return;
            }
            string destFile = srcFile +".enc";
            if( GenerateEncryptedFile(srcFile, destFile))
            {
                EditorUtility.DisplayDialog("Generated", "Generated " + destFile, "ok");
            }
        }

        /// <summary>
        /// ASTC / KTX / DDS ファイルを指定して、暗号化ファイルの作成
        /// </summary>
        [MenuItem("Tools/RuntimeCompressedTexturePacker/GenerateEncryptTexture/SelectDirectory")]
        public static void SelectTargetDirectory()
        {
            var srcDirectory = EditorUtility.OpenFolderPanel("Select Target Direcotyr","","");
            if (string.IsNullOrEmpty(srcDirectory))
            {
                return;
            }
            var files = System.IO.Directory.GetFiles(srcDirectory);
            int count = 0;
            foreach (var file in files)
            {
                string lowerStr = file.ToLower();
                if( !lowerStr.EndsWith(".astc") && !lowerStr.EndsWith(".ktx") && !lowerStr.EndsWith(".dds"))
                {
                    continue;
                }
                string destFile = file + ".enc";
                if (GenerateEncryptedFile(file, destFile))
                {
                    ++count;
                }
            }
            EditorUtility.DisplayDialog("Generated", "Generate " + count + " files.", "ok");

        }

        /// <summary>
        /// 暗号化されたファイルの生成
        /// </summary>
        /// <param name="srcPath"></param>
        /// <param name="destPath"></param>
        /// <returns></returns>
        public static bool GenerateEncryptedFile(string srcPath, string destPath)
        {
            using (var data = GenerateEncryptedTextureData(srcPath))
            {
                if(!data.IsCreated || data.Length < 16)
                {
                    return false;
                }

                using (FileStream fs = new FileStream(destPath, FileMode.Create))
                {
                    fs.Write( data.AsSpan() );
                }
            }
            return true;
        }

        /// <summary>
        /// ファイルを読み込んで、暗号化データを作成して返します
        /// </summary>
        /// <param name="srcFilePath">読み込むファイルパス</param>
        /// <returns>暗号化されたデータ</returns>
        private static NativeArray<byte> GenerateEncryptedTextureData(string srcFilePath)
        {
            NativeArray<byte> data;
            using (var originalFileBinary = UnsafeFileReadUtility.LoadFileSync(srcFilePath, Allocator.Temp))
            {
                ITextureFileFormat textureFileFormat = TextureFileFormatUtility.GetTextureFileFormatObject(originalFileBinary);
                if (!textureFileFormat.IsValid)
                {
                    return new NativeArray<byte>();
                }

                var imgData = textureFileFormat.GeImageDataWithoutMipmap(originalFileBinary);
                data = new NativeArray<byte>( imgData.Length + 16 , Allocator.Temp );

                WriteUint(data, 0, Signature);
                WriteUint(data, 4, (uint)textureFileFormat.width);
                WriteUint(data, 8, (uint)textureFileFormat.height);
                WriteUint(data, 12, (uint)textureFileFormat.textureFormat);
                NativeArray<byte>.Copy(imgData, 0, data, 16, data.Length);
                EncryptData(imgData, 16,EncryptKey);
            }
            return data;
        }

        /// <summary>
        /// Uint書き込み
        /// </summary>
        /// <param name="dst">書き込み先</param>
        /// <param name="offset">オフセット</param>
        /// <param name="val">書き込む値</param>
        private static void WriteUint(NativeArray<byte> dst, int offset, uint val)
        {
            dst[offset + 0] = (byte)(val & 0xff);
            dst[offset + 1] = (byte)((val >> 8 )& 0xff);
            dst[offset + 2] = (byte)((val >> 16) & 0xff);
            dst[offset + 3] = (byte)((val >> 24) & 0xff);
        }

        /// <summary>
        /// 暗号化処理（単純なXOR)
        /// </summary>
        /// <param name="data">変換先</param>
        /// <param name="offset">変換元</param>
        /// <param name="xorKey">XOR KEY</param>

        private static unsafe void EncryptData(NativeArray<byte> data,int offset,uint xorKey)
        {
            uint *ptr = (uint*)((byte*)data.GetUnsafePtr() + offset);
            int endIdx = data.Length / 4;
            for (int i = offset /4 ; i < endIdx; i++)
            {
                *ptr = *ptr ^ xorKey;
                ++ptr;
            }
        }

        
    }
}