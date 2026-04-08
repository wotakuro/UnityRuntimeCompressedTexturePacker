#if (!UNITY_EDITOR &&  UNITY_WEBGL )
#define WEB_RUNTIME_BUILD 
#endif

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UTJ.RuntimeCompressedTexturePacker.Format;
using System.Linq;
using System;
using UnityEngine.Networking;



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
        /// ロード完了時の関数
        /// </summary>
        /// <param name="loadSprites">ロードされたスプライト一覧</param>
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

        /// ロードプロセス中？
        private bool isOnLoadingProcess = false;


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
            if (this.generatedSpriteByFile != null)
            {
                this.generatedSpriteByFile.Clear();
            }
        }

#if UNITY_WEBGL

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
            InitBeforeLoadStart(files, true);

            foreach (var file in this.readFileListBuffer)
            {
                // 既に登録済みファイル
                if (this.generatedSpriteByFile.TryGetValue(file, out Sprite sprite))
                {
                    this.generatedSpritesBuffer.Add(sprite);
                    continue;
                }
                int size = 0;
                using (var webRequest = UnityWebRequest.Get(file))
                {
                    var operation = webRequest.SendWebRequest();

                    while (!operation.isDone)
                    {
                        yield return null;
                    }

                    if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                        webRequest.result == UnityWebRequest.Result.ProtocolError)
                    {
                        if (onFailedFile != null)
                        {
                            onFailedFile(file, 0, 0);
                        }
                        this.generatedSpritesBuffer.Add(null);
                        continue;
                    }
                    size = UnsafeFileReadUtility.GetDataSizeFromWebRequest(webRequest);
                    if (!this.fileReadBuffer.IsCreated)
                    {
                        this.fileReadBuffer = new NativeArray<byte>((int)size, Allocator.Persistent);
                    }
                    else if (this.fileReadBuffer.Length < size)
                    {
                        this.fileReadBuffer.Dispose();
                        this.fileReadBuffer = new NativeArray<byte>(size, Allocator.Persistent);
                    }
                    UnsafeFileReadUtility.GetDataFromWebRequest(webRequest, this.fileReadBuffer);
                }
                CreateSprite(file, this.fileReadBuffer, size,onFailedFile);
            }
            compressedTexturePacker.ApplyToTexture();
            if (this.fileReadBuffer.IsCreated)
            {
                this.fileReadBuffer.Dispose();
            }
            this.ExecuteAfterLoadEnd();
            if (onComplete != null)
            {
                onComplete(generatedSpritesBuffer);
            }
        }



        /// <summary>
        /// 非同期ロード処理
        /// </summary>
        /// <param name="files"></param>
        /// <param name="onComplete"></param>
        /// <param name="onFailedFile"></param>
        /// <returns></returns>
        public async Awaitable<List<Sprite>> LoadAndPackAsync(IEnumerable<string> files,
            LoadingComplete onComplete = null,
            TexturePackingError onFailedFile = null)
        {
            InitBeforeLoadStart(files, true);
            foreach (var file in this.readFileListBuffer)
            {
                // 既に登録済みファイル
                if (this.generatedSpriteByFile.TryGetValue(file, out Sprite sprite))
                {
                    this.generatedSpritesBuffer.Add(sprite);
                    continue;
                }

                int size = 0;
                using (var webRequest = UnityWebRequest.Get(file))
                {
                    var operation = webRequest.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await Awaitable.NextFrameAsync();
                    }

                    if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                        webRequest.result == UnityWebRequest.Result.ProtocolError)
                    {
                        if (onFailedFile != null)
                        {
                            onFailedFile(file, 0, 0);
                        }
                        this.generatedSpritesBuffer.Add(null);
                        continue;
                    }
                    size = UnsafeFileReadUtility.GetDataSizeFromWebRequest(webRequest);

                    if (!this.fileReadBuffer.IsCreated)
                    {
                        this.fileReadBuffer = new NativeArray<byte>(size, Allocator.Persistent);
                    }
                    else if (this.fileReadBuffer.Length < size)
                    {
                        this.fileReadBuffer.Dispose();
                        this.fileReadBuffer = new NativeArray<byte>(size, Allocator.Persistent);
                    }
                    UnsafeFileReadUtility.GetDataFromWebRequest(webRequest, this.fileReadBuffer);
                }
                CreateSprite(file, fileReadBuffer,size, onFailedFile);
            }
            compressedTexturePacker.ApplyToTexture();
            if (fileReadBuffer.IsCreated)
            {
                fileReadBuffer.Dispose();
            }
            this.ExecuteAfterLoadEnd();
            if (onComplete != null)
            {
                onComplete(generatedSpritesBuffer);
            }
            return generatedSpritesBuffer;
        }


#else
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
            InitBeforeLoadStart(files,true);

            foreach (var file in this.readFileListBuffer)
            {
                // 既に登録済みファイル
                if (this.generatedSpriteByFile.TryGetValue(file, out Sprite sprite))
                {
                    this.generatedSpritesBuffer.Add(sprite);
                    continue;
                }
                long fileSize = UnsafeFileReadUtility.GetFileSize(file);
                if (fileSize <= 0)
                {
                    if(onFailedFile != null)
                    {
                        onFailedFile(file, 0, 0);
                    }
                    this.generatedSpritesBuffer.Add(null);
                    continue;
                }
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
                CreateSprite(file, fileReadBuffer,(int)readHandle.GetBytesRead(), onFailedFile);
            }
            compressedTexturePacker.ApplyToTexture();
            if (fileReadBuffer.IsCreated)
            {
                fileReadBuffer.Dispose();
            }
            this.ExecuteAfterLoadEnd();
            if (onComplete != null)
            {
                onComplete(generatedSpritesBuffer);
            }
        }



        /// <summary>
        /// 非同期ロード処理
        /// </summary>
        /// <param name="files"></param>
        /// <param name="onComplete"></param>
        /// <param name="onFailedFile"></param>
        /// <returns></returns>
        public async Awaitable<List<Sprite>> LoadAndPackAsync(IEnumerable<string> files, 
            LoadingComplete onComplete = null,
            TexturePackingError onFailedFile = null)
        {
            InitBeforeLoadStart(files, true);
            foreach (var file in this.readFileListBuffer)
            {
                // 既に登録済みファイル
                if (this.generatedSpriteByFile.TryGetValue(file, out Sprite sprite))
                {
                    this.generatedSpritesBuffer.Add(sprite);
                    continue;
                }
                long fileSize = UnsafeFileReadUtility.GetFileSize(file);
                if (fileSize <= 0)
                {
                    if (onFailedFile != null)
                    {
                        onFailedFile(file, 0, 0);
                    }
                    this.generatedSpritesBuffer.Add(null);
                    continue;
                }
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
                    await Awaitable.NextFrameAsync();
                }
                CreateSprite(file, fileReadBuffer, (int)readHandle.GetBytesRead(), onFailedFile);
            }
            compressedTexturePacker.ApplyToTexture();
            if (fileReadBuffer.IsCreated)
            {
                fileReadBuffer.Dispose();
            }
            this.ExecuteAfterLoadEnd();
            if (onComplete != null)
            {
                onComplete(generatedSpritesBuffer);
            }
            return generatedSpritesBuffer;
        }
#endif



        /// <summary>
        /// ファイル読み込みと同期
        /// </summary>
        /// <param name="files">対象ファイル</param>
        /// <param name="onComplete">完了時の呼び出し処理</param>
        /// <param name="onFailedFile">失敗時の呼び出し</param>
        public List<Sprite> LoadAndPack(IEnumerable<string> files, LoadingComplete onComplete = null,
            TexturePackingError onFailedFile = null)
        {
#if UNITY_WEBGL 
            throw new NotImplementedException("Web runtimes do not support synchronous file access.");
#endif
            InitBeforeLoadStart(files,false);
            NativeArray<long> fileSizes = new NativeArray<long>( files.Count(),Allocator.Temp);
            long biggestSize = 0;
            int idx = 0;
            foreach (var file in files)
            {
                if (!this.generatedSpriteByFile.ContainsKey(file))
                {
                    long fileSize = UnsafeFileReadUtility.GetFileSize(file);
                    if (fileSize > biggestSize)
                    {
                        biggestSize = fileSize;
                    }
                    fileSizes[idx] = fileSize;
                }
                ++idx;
            }
            using (var buffer = new NativeArray<byte>((int)biggestSize, Allocator.Temp))
            {
                idx = 0;
                foreach (var file in files)
                {
                    // 既に登録済みファイル
                    if (this.generatedSpriteByFile.TryGetValue(file, out Sprite sprite))
                    {
                        this.generatedSpritesBuffer.Add(sprite);
                        continue;
                    }
                    if (fileSizes[idx] > 0)
                    {
                        long bytes = UnsafeFileReadUtility.LoadFileSync(file, buffer, fileSizes[idx]);
                        if(bytes <= 0)
                        {
                            if (onFailedFile != null)
                            {
                                onFailedFile(file, 0, 0);
                            }
                            this.generatedSpritesBuffer.Add(null);
                            continue;
                        }
                        var name = Path.GetFileNameWithoutExtension(file);
                        CreateSprite(file, buffer,(int)bytes, onFailedFile);
                    }
                    ++idx;
                }
            }
            compressedTexturePacker.ApplyToTexture();
            this.ExecuteAfterLoadEnd();
            if (onComplete != null)
            {
                onComplete(generatedSpritesBuffer);
            }
            return this.generatedSpritesBuffer;
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
        /// ロード開始前の初期化処理
        /// </summary>
        private void InitBeforeLoadStart(IEnumerable<string> files,bool copyFileList)
        {
            if (this.isOnLoadingProcess)
            {
                throw new AlreadyRunningLoadingException();
            }
            if (this.generatedSpritesBuffer == null)
            {
                this.generatedSpritesBuffer = new List<Sprite>();
            }
            this.generatedSpritesBuffer.Clear();
            if (this.generatedSpriteByFile == null)
            {
                this.generatedSpriteByFile = new Dictionary<string, Sprite>();
            }

            if (copyFileList)
            {
                if (this.readFileListBuffer == null)
                {
                    this.readFileListBuffer = new List<string>();
                }
                this.readFileListBuffer.Clear();
                foreach (var file in files)
                {
                    this.readFileListBuffer.Add(file);
                }
            }

            this.isOnLoadingProcess = true;
        }

        /// <summary>
        /// ロード完了後に処理
        /// </summary>
        private void ExecuteAfterLoadEnd()
        {
            this.isOnLoadingProcess = false;
        }

        /// <summary>
        /// 読み込んだバイナリからSpriteを作成します
        /// </summary>
        /// <param name="file">ファイル名</param>
        /// <param name="fileDataBuffer">ファイルの中身のバッファ</param>
        /// <param name="dataLength">実際のデータの長さ</param>
        /// <param name="onFailedFile">失敗時の呼び出し</param>
        /// <returns>作成したSpriteを返します</returns>
        private Sprite CreateSprite(string file, NativeArray<byte> fileDataBuffer,int dataLength, TexturePackingError onFailedFile)
        {
            if(dataLength == 0)
            {
                if (onFailedFile != null)
                {
                    onFailedFile(file, 0, 0);
                }
                this.generatedSpritesBuffer.Add(null);
                return null;
            }
            using (var fileBinary = fileDataBuffer.GetSubArray(0, dataLength))
            {
                ITextureFileFormat textureFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);
                if (!textureFile.LoadHeader(fileBinary))
                {
                    if (onFailedFile != null)
                    {
                        onFailedFile(file, 0, 0);
                    }
                    this.generatedSpritesBuffer.Add(null);
                    return null;
                }
                // format Check
                if (textureFile.textureFormat != this.compressedTexturePacker.textureFormat)
                {
                    if (onFailedFile != null)
                    {
                        onFailedFile(file, texture.width, texture.height);
                    }
                    this.generatedSpritesBuffer.Add(null);
#if DEBUG
                    Debug.LogError("TextureFormat error " + this.compressedTexturePacker.textureFormat + "<-" + textureFile.textureFormat);
#endif
                    return null;
                }

                var name = Path.GetFileNameWithoutExtension(file);

                using (var textureBodyData = textureFile.GeImageDataWithoutMipmap(fileBinary))
                {
                    var rect = compressedTexturePacker.AppendTextureData(textureFile.width, textureFile.height, textureBodyData);
                    if (rect.width <= 0 || rect.height <= 0)
                    {
                        if (onFailedFile != null)
                        {
                            onFailedFile(file, textureFile.width, textureFile.height);
                        }
                        this.generatedSpritesBuffer.Add(null);
                    }
                    else
                    {
                        var texture2d = compressedTexturePacker.texture2D;
                        var sprite = Sprite.Create(texture2d, rect, new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect);
                        sprite.name = name;
                        this.generatedSpritesBuffer.Add(sprite);
                        this.generatedSpriteByFile.Add(file, sprite);
                        return sprite;
                    }
                }
            }
            return null;
        }
    }
}