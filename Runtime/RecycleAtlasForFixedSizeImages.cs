#if (!UNITY_EDITOR &&  UNITY_WEBGL )
#define WEB_RUNTIME_BUILD 
#endif

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UTJ.RuntimeCompressedTexturePacker.Format;
using System.Linq;
using UTJ.RuntimeCompressedTexturePacker.Packing;
using Unity.IO.LowLevel.Unsafe; // not web
using UnityEngine.Networking;
using System.Runtime.CompilerServices; // WebOnly


namespace UTJ.RuntimeCompressedTexturePacker
{

    /// <summary>
    /// ファイルリストを渡して自動的にAtlasを生成してくれます
    /// </summary>
    public class RecycleAtlasForFixedSizeImages : System.IDisposable
    {
#if RCTP_DEVMODE
        public static RecycleAtlasForFixedSizeImages Instance { get; private set; }
#endif
        private enum EState
        {
            None,
            Loading,
        }
        // リクエストされたファイル
        private struct RequestFile
        {
            public string file;
            public uint orderValue;
            public Sprite sprite;
            public int fileSize;
            public int frame;
        }

        // 待ちリスト比較用
        private class ComparerForWaitList : IComparer<string>
        {
            private Dictionary<string, RequestFile> requestedFiles;

            public ComparerForWaitList(Dictionary<string, RequestFile> requests)
            {
                this.requestedFiles = requests;
            }
            public int Compare(string x, string y)
            {
                RequestFile xObj,yObj;
                if (!requestedFiles.TryGetValue(x, out xObj))
                {
                    if (!requestedFiles.ContainsKey(y))
                    {
                        return 0;
                    }
                    return -1;
                }
                if (!requestedFiles.TryGetValue(y, out yObj))
                {
                    return 1;
                }
                return (int)xObj.orderValue - (int)yObj.orderValue;
            }
        }

        // 現在の順番
        private uint currentOrderValue;

        /// リクエストされたファイル一覧
        private Dictionary<string, RequestFile> requestedFiles = new Dictionary<string, RequestFile>();

        /// ロードのキュー
        private List<string> waitLoadList = new List<string>();

        // StringのKey用のBuffer
        private List<string> keyBuffer = new List<string>();
        // 一個前に実行したときのTime.frameCount
        private int prevTimeFrameCount = 0;
#if UNITY_WEBGL
        // WebRequest
        private UnityWebRequest webRequest;

        private UnityWebRequestAsyncOperation webRequestAsync;
#else
        // ReadHandle
        private ReadHandle readHandle;
#endif

        // 現在ロード中のファイル
        private string currentLoadingFile;

        /// Packingを行うオブジェクト
        private CompressedTexturePacker compressedTexturePacker;
        /// FileRead用のBuffer
        private NativeArray<byte> fileReadBuffer;
        /// 飽きを探すアルゴリズム
        private IRectResolveAlgorithm resolveAlgorithm;

        ///  ソート用オブジェクト
        private ComparerForWaitList comparerForWaitList;

        // 現在のState
        private EState state = EState.None;

        /// 実際のGridの幅（テクスチャブロックサイズを考慮)
        private int actualGridWidth;
        /// 実際のGridの高さ（テクスチャブロックサイズを考慮)
        private int actualGridHeight;


        /// <summary>
        /// Texture2D
        /// </summary>
        public Texture2D texture2D
        {
            get
            {
                return this.compressedTexturePacker.texture2D;
            }
        }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="textureFormat"></param>
        /// <param name="gridWidth"></param>
        /// <param name="gridHeight"></param>
        public RecycleAtlasForFixedSizeImages(int width, int height, TextureFormat textureFormat, int gridWidth, int gridHeight,int margin=0)
        {
            int blockX, blockY, blockSize;
            TextureFileFormatUtility.GetBlockInfo(textureFormat, out blockX, out blockY, out blockSize);

            this.actualGridWidth = ((gridWidth + blockX - 1 + margin) / blockX) * blockX;
            this.actualGridHeight = ((gridHeight + blockY - 1 + margin) / blockY) * blockY;

            this.resolveAlgorithm = new GridMapPacking(actualGridWidth, actualGridHeight);
            this.compressedTexturePacker = new CompressedTexturePacker(width, height, textureFormat,
                false, this.resolveAlgorithm, margin);

#if RCTP_DEVMODE
            Instance = this;
#endif
        }

        /// <summary>
        /// Spriteをリクエストします。毎フレーム呼んでください
        /// </summary>
        /// <param name="path">ファイルのパス</param>
        /// <returns>作成されたSprite（nullは作成中)</returns>
        public Sprite Request(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            // 
            int currentFrame = Time.frameCount;
            RequestFile requestFile;
            if (requestedFiles.TryGetValue(path, out requestFile))
            {
                ++currentOrderValue;
                if (requestFile.frame != currentFrame)
                {
                    requestFile.orderValue = currentOrderValue;
                    requestFile.frame = currentFrame;
                    this.requestedFiles[path] = requestFile;
                }
                UpdateIfFrameChaged();
                return requestFile.sprite;
            }
#if UNITY_WEBGL
            requestFile = new RequestFile()
            {
                file = path,
                orderValue = currentOrderValue,
                sprite = null,
                fileSize = 0,
                frame = Time.frameCount,
            };

#else
            requestFile = new RequestFile()
            {
                file = path,
                orderValue = currentOrderValue,
                sprite = null,
                fileSize = (int)UnsafeFileReadUtility.GetFileSize(path),
                frame = Time.frameCount,
            };
            if (requestFile.fileSize <= 0)
            {
                Debug.LogWarning("FileSize zero " + path);
                return null;
            }
#endif
            this.requestedFiles.Add(path, requestFile);
            this.AppendLoadingWaintList(path);

            ++currentOrderValue;
            UpdateIfFrameChaged();
            return null;
        }


        /// <summary>
        /// ロード待ちのリストから削除を行います
        /// </summary>
        /// <param name="path">リストから削除したいファイル</param>
        public void RemoveFromWaitList(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            if (this.currentLoadingFile == path)
            {
                return;
            }

            RequestFile requestFile;
            if (!requestedFiles.TryGetValue(path, out requestFile))
            {
                return;
            }
            if (requestFile.sprite == null)
            {
                int length = this.waitLoadList.Count;
                for (int i = 0; i < length; ++i)
                {
                    if (this.waitLoadList[i] == path)
                    {
                        this.waitLoadList.RemoveAt(i);
                        break;
                    }
                }
                this.requestedFiles.Remove(path);
            }
        }

        /// <summary>
        /// Dispose処理
        /// </summary>
        public void Dispose()
        {
            if (this.fileReadBuffer.IsCreated)
            {
                this.fileReadBuffer.Dispose();
            }
            if (this.resolveAlgorithm != null)
            {
                this.resolveAlgorithm.Dispose();
            }
            if (this.compressedTexturePacker != null)
            {
                this.compressedTexturePacker.Dispose();
            }
#if UNITY_WEBGL
            if(this.webRequest != null)
            {
                this.webRequest.Dispose();
                this.webRequest = null;
            }
#endif

#if RCTP_DEVMODE
            Instance = null;
#endif
        }

        private void UpdateIfFrameChaged()
        {
            if (Time.frameCount == this.prevTimeFrameCount)
            {
                return;
            }
            this.prevTimeFrameCount = Time.frameCount;
            if(this.currentOrderValue > uint.MaxValue / 4)
            {
                this.SortOutOrderNumber();
            }

            switch (this.state)
            {
                case EState.None:
                    LoadRequestFromQueue();
                    break;
                case EState.Loading:
                    WaitLoadProcess();
                    break;
            }
        }
#if UNITY_WEBGL
        /// <summary>
        /// Queueにあるものから、ロードをリクエストします
        /// </summary>
        private void LoadRequestFromQueue()
        {
            if (IsLoadWaitListIsEmpty())
            {
                return;
            }

            RequestFile requestFile;
            this.currentLoadingFile = PickupLoadingWaitList();
            if (!this.requestedFiles.TryGetValue(this.currentLoadingFile, out requestFile))
            {
                return;
            }
            if (requestFile.sprite != null)
            {
                return;
            }

            bool stateChange = true;
            if (!this.compressedTexturePacker.CanAppendTextureData(this.actualGridWidth, this.actualGridHeight))
            {
                if (ShouldAppendCurrentData(currentLoadingFile))
                {
                    RemoveOldSprite();
                }
                else
                {
                    currentLoadingFile = null;
                    stateChange = false;
                }
            }

            if (this.webRequest != null)
            {
                this.webRequest.Dispose();
                this.webRequest = null;
            }
            this.webRequest = UnityWebRequest.Get(requestFile.file);
            this.webRequestAsync = this.webRequest.SendWebRequest();


            if (stateChange)
            {
                this.state = EState.Loading;
            }
        }

        /// <summary>
        /// ロードの待ち中の処理
        /// </summary>
        private void WaitLoadProcess()
        {
            if (!webRequestAsync.isDone)
            {
                return;
            }
            if(this.webRequest == null)
            {
                return;
            }
            // エラーハンドリング
            if (this.webRequest.result == UnityWebRequest.Result.ConnectionError ||
                this.webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                this.state = EState.None;

                if (this.webRequest != null)
                {
                    this.webRequest.Dispose();
                    this.webRequest = null;
                }
                return;
            }
            if (!this.requestedFiles.ContainsKey(this.currentLoadingFile))
            {
                this.currentLoadingFile = "";
                this.state = EState.None;
                return;
            }

            int size = UnsafeFileReadUtility.GetDataSizeFromWebRequest(this.webRequest);
            if (!this.fileReadBuffer.IsCreated)
            {
                this.fileReadBuffer = new NativeArray<byte>(size, Allocator.Persistent);
            }
            else if (fileReadBuffer.Length < size)
            {
                this.fileReadBuffer.Dispose();
                this.fileReadBuffer = new NativeArray<byte>(size, Allocator.Persistent);
            }
            UnsafeFileReadUtility.GetDataFromWebRequest(webRequest, this.fileReadBuffer);

            if (this.webRequest != null)
            {
                this.webRequest.Dispose();
                this.webRequest = null;
            }

            ITextureFileFormat fileFormat = TextureFileFormatUtility.GetTextureFileFormatObject(this.fileReadBuffer);
            fileFormat.LoadHeader(this.fileReadBuffer);

            // fileformat check
            if (fileFormat.textureFormat != this.compressedTexturePacker.textureFormat)
            {
#if DEBUG
                Debug.LogError("TextureFormat error " + this.compressedTexturePacker.textureFormat + "<-" + fileFormat.textureFormat);
#endif
                this.currentLoadingFile = "";
                this.state = EState.None;
                return;
            }

            if (!this.compressedTexturePacker.CanAppendTextureData(this.actualGridWidth, this.actualGridHeight))
            {
                RemoveOldSprite();
            }
            using (var textureBodyData = fileFormat.GeImageDataWithoutMipmap(this.fileReadBuffer))
            {
                var rect = this.compressedTexturePacker.AppendTextureData(fileFormat.width, fileFormat.height,
                    textureBodyData);
                if (rect.width <= 0 || rect.height <= 0)
                {
                    Debug.LogError("Failed Add data " + currentLoadingFile);
                }
                this.compressedTexturePacker.ApplyToTexture();
                var sprite = Sprite.Create(this.compressedTexturePacker.texture2D, rect, new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect);
                if (this.requestedFiles.TryGetValue(currentLoadingFile, out var file))
                {
                    file.sprite = sprite;
                    this.requestedFiles[currentLoadingFile] = file;
                }
                this.currentLoadingFile = "";
                this.state = EState.None;
            }

        }

#else
        /// <summary>
        /// Queueにあるものから、ロードをリクエストします
        /// </summary>
        private void LoadRequestFromQueue()
        {
            if (IsLoadWaitListIsEmpty())
            {
                return;
            }
            RequestFile requestFile;
            this.currentLoadingFile = this.PickupLoadingWaitList();
            if (!this.requestedFiles.TryGetValue(this.currentLoadingFile, out requestFile))
            {
                return;
            }
            if (requestFile.sprite != null)
            {
                return;
            }

            if (!this.fileReadBuffer.IsCreated)
            {
                this.fileReadBuffer = new NativeArray<byte>(requestFile.fileSize, Allocator.Persistent);
            }
            else if (fileReadBuffer.Length < requestFile.fileSize)
            {
                this.fileReadBuffer.Dispose();
                this.fileReadBuffer = new NativeArray<byte>(requestFile.fileSize, Allocator.Persistent);
            }
            this.readHandle = UnsafeFileReadUtility.RequestLoad(requestFile.file, this.fileReadBuffer, requestFile.fileSize);
            this.state = EState.Loading;
            if (!this.compressedTexturePacker.CanAppendTextureData(this.actualGridWidth, this.actualGridHeight))
            {
                RemoveOldSprite();
            }

        }

        /// <summary>
        /// ロードの待ち中の処理
        /// </summary>
        private void WaitLoadProcess()
        {
            if (this.readHandle.Status == ReadStatus.Complete)
            {
                if (!this.requestedFiles.ContainsKey(this.currentLoadingFile))
                {
                    this.currentLoadingFile = "";
                    this.state = EState.None;
                    return;
                }

                ITextureFileFormat fileFormat = TextureFileFormatUtility.GetTextureFileFormatObject(this.fileReadBuffer);
                fileFormat.LoadHeader(this.fileReadBuffer);

                // fileformat check
                if (fileFormat.textureFormat != this.compressedTexturePacker.textureFormat)
                {
#if DEBUG
                    Debug.LogError("TextureFormat error " + this.compressedTexturePacker.textureFormat + "<-" + fileFormat.textureFormat);
#endif
                    this.currentLoadingFile = "";
                    this.state = EState.None;
                    return;
                }

                if (!this.compressedTexturePacker.CanAppendTextureData(this.actualGridWidth, this.actualGridHeight))
                {
                    RemoveOldSprite();
                }
                using (var textureBodyData = fileFormat.GeImageDataWithoutMipmap(this.fileReadBuffer))
                {
                    var rect = this.compressedTexturePacker.AppendTextureData(fileFormat.width, fileFormat.height,
                        textureBodyData);
                    if (rect.width <= 0 || rect.height <= 0)
                    {
                        Debug.LogError("Failed Add data " + currentLoadingFile);
                    }
                    this.compressedTexturePacker.ApplyToTexture();
                    var sprite = Sprite.Create(this.compressedTexturePacker.texture2D, rect, new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect);
                    if (this.requestedFiles.TryGetValue(currentLoadingFile, out var file))
                    {
                        file.sprite = sprite;
                        this.requestedFiles[currentLoadingFile] = file;
                    }
                    this.currentLoadingFile = "";
                    this.state = EState.None;
                }
            }
            else if (readHandle.Status == ReadStatus.Failed || readHandle.Status == ReadStatus.Canceled || readHandle.Status == ReadStatus.Truncated)
            {
                this.state = EState.None;
            }
        }
#endif

        /// <summary>
        /// ロードリストに追加します
        /// </summary>
        /// <param name="path">追加するパス</param>
        private void AppendLoadingWaintList(string path)
        {
            this.waitLoadList.Add(path);
        }


        /// <summary>
        /// ロードリストから一個選びます
        /// </summary>
        /// <returns>ロード待ちから一つ取り出します</returns>
        private string PickupLoadingWaitList()
        {
            if (this.waitLoadList.Count == 0)
            {
                return null;
            }

            this.SortOutOrderNumber();
            if (this.comparerForWaitList == null)
            {
                this.comparerForWaitList = new ComparerForWaitList(this.requestedFiles);
            }
            this.waitLoadList.Sort(this.comparerForWaitList);
            int index = GetIndexForLoading();
            string path = this.waitLoadList[index];
            this.waitLoadList.RemoveAt(index);
            return path;
        }

        /// <summary>
        /// ソート済みのロードリストから、最初にロードすべきIndexを返します
        /// </summary>
        /// <returns>ロードすべきIndexを返します</returns>
        private int GetIndexForLoading()
        {
            int lastIndex = this.waitLoadList.Count - 1;
            RequestFile requested;
            if (!this.requestedFiles.TryGetValue(this.waitLoadList[lastIndex], out requested))
            {
                return lastIndex;
            }
            int frame = requested.frame;
            for (int i= lastIndex-1; i >= 0; --i)
            {
                if (!this.requestedFiles.TryGetValue(this.waitLoadList[i], out requested))
                {
                    return lastIndex;
                }
                if(frame != requested.frame)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// ロード待ちリストが空かを返します
        /// </summary>
        /// <returns>ロード待ちが空ならTrue</returns>
        private bool IsLoadWaitListIsEmpty()
        {
            return (waitLoadList.Count <= 0);
        }

        /// <summary>
        /// OrderNumberを下げます
        /// </summary>
        private void SortOutOrderNumber()
        {
            uint minimumOrderValue = uint.MaxValue;
            foreach (var requestFile in requestedFiles.Values)
            {
                if (minimumOrderValue > requestFile.orderValue)
                {
                    minimumOrderValue = requestFile.orderValue;
                }
            }
            keyBuffer.Clear();
            foreach (var key in requestedFiles.Keys)
            {
                keyBuffer.Add(key);
            }
            foreach(var key in keyBuffer)
            {
                var val = this.requestedFiles[key];
                val.orderValue = val.orderValue - minimumOrderValue;
                requestedFiles[key] = val;
            }
            this.currentOrderValue -= minimumOrderValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentFile"></param>
        /// <returns></returns>
        private bool ShouldAppendCurrentData(string currentFile)
        {
            RequestFile current;
            if(!this.requestedFiles.TryGetValue(currentFile, out current))
            {
                return false;
            }

            RequestFile oldest = new RequestFile() { orderValue = uint.MaxValue };
            foreach (var requestFile in requestedFiles.Values)
            {
                if(!requestFile.sprite)
                {
                    continue;
                }
                if (oldest.orderValue > requestFile.orderValue)
                {
                    oldest = requestFile;
                }
            }
            return (current.orderValue > oldest.orderValue);
        }

        /// <summary>
        /// 古いスプライトを削除します
        /// </summary>
        private void RemoveOldSprite()
        {
            bool isFound = false;
            while (!isFound && requestedFiles.Count > 0 )
            {
                RequestFile oldest = new RequestFile() { orderValue = uint.MaxValue };
                foreach (var requestFile in requestedFiles.Values)
                {
                    if (oldest.orderValue > requestFile.orderValue)
                    {
                        oldest = requestFile;
                    }
                }
                this.requestedFiles.Remove(oldest.file);
                if (oldest.sprite)
                {
                    RectInt rectInt = new RectInt((int)((oldest.sprite.rect.x + 0.5f) / actualGridWidth) * actualGridWidth,
                        (int)((oldest.sprite.rect.y + 0.5f) / actualGridHeight) * actualGridHeight,
                        actualGridWidth, actualGridHeight);
                    this.compressedTexturePacker.RemoveRect(rectInt);
                    isFound = true;
                }
            }
        }



#if RCTP_DEVMODE
        public uint CurrentOder => currentOrderValue;
        public string CurrentFile => currentLoadingFile;
        public List<string> LoadingQueue => this.waitLoadList;

        public int State => (int)this.state;

#if !UNITY_WEBGL
        public ReadStatus readStatus => this.readHandle.Status;
#endif

        public List<string> RequestedFiles => this.requestedFiles.Keys.ToList();

        public Vector2Int Grid => new Vector2Int(this.actualGridWidth, this.actualGridHeight);

        public uint GetOrderValueInRequestFile(string file)
        {
            RequestFile request;
            if (this.requestedFiles.TryGetValue(file, out request))
            {
                return request.orderValue;
            }
            return uint.MaxValue;
        }
        public bool IsSriteExists(string file)
        {
            RequestFile request;
            if (this.requestedFiles.TryGetValue(file, out request))
            {
                return request.sprite;
            }
            return false;
        }
#endif
    }
}