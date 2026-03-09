using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UTJ.RuntimeCompressedTexturePacker.Format;
using UTJ.RuntimeCompressedTexturePacker.Packing;
using UTJ.RuntimeCompressedTexturePacker;


namespace UTJ.RuntimeCompressedTexturePacker
{
    /// <summary>
    /// ファイルリストを渡して自動的にAtlasを生成してくれます
    /// </summary>
    public class AutoAtlasBuilder : System.IDisposable
    {
        /// <summary>
        /// TextureをロードしてPackingする時のエラー
        /// </summary>
        /// <param name="file">ファイル名</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public delegate void TexturePackingError(string file, int width, int height);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loadSprites"></param>
        public delegate void LoadingComplete(IEnumerable<Sprite> loadSprites);

        /// 生成したSpriteを一時的に保存します
        private List<Sprite> generatedSpritesBuffer = new List<Sprite>();

        /// Packingを行うオブジェクト
        private CompressedTexturePacker compressedTexturePacker;

        /// FileRead用のBuffer
        private NativeArray<byte> fileReadBuffer;

        /// <summary>
        /// AtlasTexture
        /// </summary>
        public Texture2D texture
        {
            get
            {
                return this.compressedTexturePacker.texture2D;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width">AtlasTexture全体の幅</param>
        /// <param name="height">AtlasTexture全体の高さ</param>
        /// <param name="textureFormat">テクスチャフォーマット</param>
        public AutoAtlasBuilder(int width, int height, TextureFormat textureFormat)
        {
            this.compressedTexturePacker = new CompressedTexturePacker(width, height, textureFormat);
        }

        /// <summary>
        /// バッファーを開放します。これ以上のデータ追加を行うことが出来ません
        /// </summary>
        public void ReleaseBuffers()
        {
            this.compressedTexturePacker.ReleaseTextureLowData();
            if (fileReadBuffer.IsCreated)
            {
                fileReadBuffer.Dispose();
            }
        }


        /// <summary>
        /// 非同期ロード処理
        /// </summary>
        /// <param name="files"></param>
        /// <param name="onComplete"></param>
        /// <param name="onFailedFile"></param>
        /// <returns></returns>
        public IEnumerator LoadAndPackAsyncCoroutine(IEnumerable<string> files, LoadingComplete onComplete,
            TexturePackingError onFailedFile)
        {
            return LoadAndPackAsyncAstc(files, onComplete, onFailedFile);
        }

        /// <summary>
        /// Dispose処理
        /// </summary>
        public void Dispose()
        {
            if (this.compressedTexturePacker != null)
            {
                this.compressedTexturePacker.Dispose();
            }
            if (fileReadBuffer.IsCreated)
            {
                fileReadBuffer.Dispose();
            }
        }


        /// ASTCの非同期ロード処理
        private IEnumerator LoadAndPackAsyncAstc(IEnumerable<string> files, LoadingComplete onComplete,
            TexturePackingError onFailedFile)
        {
            generatedSpritesBuffer.Clear();

            foreach (var file in files)
            {
                long fileSize = UnsafeFileReadUtility.GetFileSize(file);
                if (!fileReadBuffer.IsCreated)
                {
                    fileReadBuffer = new NativeArray<byte>((int)fileSize, Allocator.Persistent);
                }
                else if (fileReadBuffer.Length < fileSize)
                {
                    fileReadBuffer.Dispose();
                    fileReadBuffer = new NativeArray<byte>((int)fileSize, Allocator.Persistent);
                }


                var readHandle = UnsafeFileReadUtility.RequestLoad(file, fileReadBuffer, fileSize);
                while (!readHandle.JobHandle.IsCompleted)
                {
                    yield return null;
                }
                AstcTextureFormat astc = new AstcTextureFormat();
                if (!astc.LoadHeader(fileReadBuffer))
                {
                    if (onFailedFile != null)
                    {
                        onFailedFile(file, -1, -1);
                    }
                    continue;
                }
                var name = Path.GetFileNameWithoutExtension(file);

                var textureBodyData = fileReadBuffer.GetSubArray(16, (int)readHandle.GetBytesRead() - 16);
                {
                    var rect = compressedTexturePacker.AppendTextureData(name, (int)astc.dim_x, (int)astc.dim_y, textureBodyData);
                    if (rect.width <= 0 || rect.height <= 0)
                    {
                        if (onFailedFile != null)
                        {
                            onFailedFile(file, (int)astc.dim_x, (int)astc.dim_y);
                        }
                    }
                    else
                    {
                        var texture2d = compressedTexturePacker.texture2D;
                        var sprite = Sprite.Create(texture2d, rect, new Vector2(0.5f, 0.5f));
                        sprite.name = name;
                        this.generatedSpritesBuffer.Add(sprite);
                    }
                }
            }
            compressedTexturePacker.ApplyToTexture();
            if (onComplete != null)
            {
                onComplete(generatedSpritesBuffer);
            }
            if (fileReadBuffer.IsCreated)
            {
                fileReadBuffer.Dispose();
            }
        }


    }
}