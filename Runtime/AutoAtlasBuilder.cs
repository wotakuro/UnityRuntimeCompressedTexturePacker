using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UTJ.RuntimeCompressedTexturePacker.Format;
using System.Linq;


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

        /// ファイル名とスプライトの対応表
        private Dictionary<string, Sprite> generatedSpriteByFile;

        /// 生成したSpriteを一時的に保存します
        private List<Sprite> generatedSpritesBuffer;

        /// Packingを行うオブジェクト
        private CompressedTexturePacker compressedTexturePacker;

        /// FileRead用のBuffer
        private NativeArray<byte> fileReadBuffer;

        /// AsyncRead時などでのファイルリストバッファ
        private List<string> readFileListBuffer;


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
            InitBeforeLoadStart(files);

            foreach (var file in this.readFileListBuffer)
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
                CreateSprite(file, fileReadBuffer, onFailedFile);
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

        /// <summary>
        /// ファイル読み込みと同期
        /// </summary>
        /// <param name="files">対象ファイル</param>
        /// <param name="onComplete">完了時の呼び出し処理</param>
        /// <param name="onFailedFile">失敗時の呼び出し</param>
        public void LoadAndPack(IEnumerable<string> files, LoadingComplete onComplete,
            TexturePackingError onFailedFile)
        {
            InitBeforeLoadStart(files);
            NativeArray<long> fileSizes = new NativeArray<long>( files.Count(),Allocator.Temp);
            long biggestSize = 0;
            int idx = 0;
            foreach (var file in this.readFileListBuffer)
            {
                long fileSize = UnsafeFileReadUtility.GetFileSize(file);
                if(fileSize > biggestSize)
                {
                    biggestSize = fileSize;
                }
                fileSizes[idx] = fileSize;
                ++idx;
            }
            using(var buffer = new NativeArray<byte>((int)biggestSize, Allocator.Temp))
            {
                idx = 0;
                foreach (var file in this.readFileListBuffer)
                {
                    UnsafeFileReadUtility.LoadFileSync(file, buffer, fileSizes[idx]);
                    var name = Path.GetFileNameWithoutExtension(file);
                    ++idx;
                    CreateSprite(file, buffer, onFailedFile);
                }
            }
            compressedTexturePacker.ApplyToTexture();
            if (onComplete != null)
            {
                onComplete(generatedSpritesBuffer);
            }
        }

        /// <summary>
        /// ロード開始前の初期化処理
        /// </summary>
        private void InitBeforeLoadStart(IEnumerable<string> files)
        {
            if (this.generatedSpritesBuffer == null)
            {
                this.generatedSpritesBuffer = new List<Sprite>();
            }
            this.generatedSpritesBuffer.Clear();
            if (this.generatedSpriteByFile == null)
            {
                this.generatedSpriteByFile = new Dictionary<string, Sprite>();
            }

            if (this.readFileListBuffer == null)
            {
                this.readFileListBuffer = new List<string>();
            }
            this.readFileListBuffer.Clear();
            foreach (var file in files)
            {
                if (!this.generatedSpriteByFile.ContainsKey(file))
                {
                    this.readFileListBuffer.Add(file);
                }
            }
        }

        /// <summary>
        /// 読み込んだバイナリからSpriteを作成します
        /// </summary>
        /// <param name="file">ファイル名</param>
        /// <param name="fileDataBuffer">ファイルの中身のバッファ</param>
        /// <param name="onFailedFile">失敗時の呼び出し</param>
        /// <returns>作成したSpriteを返します</returns>
        private Sprite CreateSprite(string file, NativeArray<byte> fileDataBuffer, TexturePackingError onFailedFile)
        {
            ITextureFileFormat textureFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileDataBuffer);
            if (!textureFile.LoadHeader(fileDataBuffer))
            {
                if (onFailedFile != null)
                {
                    onFailedFile(file, -1, -1);
                }
                return null;
            }
            // format Check
            if (textureFile.textureFormat != this.compressedTexturePacker.textureFormat)
            {
                if (onFailedFile != null)
                {
                    onFailedFile(file, texture.width, texture.height);
                }
                return null;
            }

            var name = Path.GetFileNameWithoutExtension(file);

            var textureBodyData = textureFile.GeImageDataWithoutMipmap(fileDataBuffer);
            {
                var rect = compressedTexturePacker.AppendTextureData(textureFile.width, textureFile.height, textureBodyData);
                if (rect.width <= 0 || rect.height <= 0)
                {
                    if (onFailedFile != null)
                    {
                        onFailedFile(file, textureFile.width, textureFile.height);
                    }
                }
                else
                {
                    var texture2d = compressedTexturePacker.texture2D;
                    var sprite = Sprite.Create(texture2d, rect, new Vector2(0.5f, 0.5f));
                    sprite.name = name;
                    this.generatedSpritesBuffer.Add(sprite);
                    this.generatedSpriteByFile.Add(file, sprite);
                    return sprite;
                }
            }
            return null;
        }
    }
}