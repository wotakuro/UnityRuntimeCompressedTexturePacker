using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UTJ.RuntimeCompressedTexturePacker.Format;
using System.Linq;
using UTJ.RuntimeCompressedTexturePacker.Packing;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine.U2D;


namespace UTJ.RuntimeCompressedTexturePacker
{



    /// <summary>
    /// ファイルリストを渡して自動的にAtlasを生成してくれます
    /// </summary>
    public class RecycleAtlasForFixedSizeImages : System.IDisposable
    {
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
        }
        // 現在の順番
        private uint currentOrderValue;

        /// ロードのキュー
        private Dictionary<string, RequestFile> requestedFiles = new Dictionary<string, RequestFile>();

        /// ロードのキュー
        private Queue<string> loadQueue = new Queue<string>();
        // 一個前に実行したときのTime.frameCount
        private int prevTimeFrameCount = 0;
        // ReadHandle
        private ReadHandle readHandle;

        // 現在ロード中のファイル
        private string currentLoadingFile;

        /// Packingを行うオブジェクト
        private CompressedTexturePacker compressedTexturePacker;
        /// FileRead用のBuffer
        private NativeArray<byte> fileReadBuffer;
        /// 飽きを探すアルゴリズム
        private IRectResolveAlgorithm resolveAlgorithm;

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
        public RecycleAtlasForFixedSizeImages(int width, int height, TextureFormat textureFormat, int gridWidth, int gridHeight)
        {
            int blockX, blockY, blockSize;
            TextureFileFormatUtility.GetBlockInfo(textureFormat, out blockX, out blockY, out blockSize);

            this.actualGridWidth = ((gridWidth + blockX - 1) / blockX) * blockX;
            this.actualGridHeight = ((gridHeight + blockY - 1) / blockY) * blockY;

            this.resolveAlgorithm = new GridMapPacking(actualGridWidth, actualGridHeight);
            this.compressedTexturePacker = new CompressedTexturePacker(width, height, textureFormat,
                false, this.resolveAlgorithm);
            this.compressedTexturePacker.marginPixel = 0;
        }

        /// <summary>
        /// Spriteをリクエストします。毎フレーム呼んでください
        /// </summary>
        /// <param name="path">ファイルのパス</param>
        /// <returns>作成されたSprite（nullは作成中)</returns>
        public Sprite Request(string path)
        {
            // 
            RequestFile requestFile;
            if (requestedFiles.TryGetValue(path, out requestFile))
            {
                ++currentOrderValue;
                requestFile.orderValue = currentOrderValue;
                this.requestedFiles[path] = requestFile;
                UpdateIfFrameChaged();
                return requestFile.sprite;
            }
            requestFile = new RequestFile()
            {
                file = path,
                orderValue = currentOrderValue,
                sprite = null,
                fileSize = (int)UnsafeFileReadUtility.GetFileSize(path)
            };
            if (requestFile.fileSize <= 0)
            {
                Debug.LogWarning("FileSize zero " + path);
                return null;
            }
            this.requestedFiles.Add(path, requestFile);
            loadQueue.Enqueue(path);

            ++currentOrderValue;
            UpdateIfFrameChaged();
            return null;
        }

        private void UpdateIfFrameChaged()
        {
            if (Time.frameCount == prevTimeFrameCount)
            {
                return;
            }
            prevTimeFrameCount = Time.frameCount;
            if (loadQueue.Count <= 0)
            {
                return;
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

        /// <summary>
        /// Queueにあるものから、ロードをリクエストします
        /// </summary>
        private void LoadRequestFromQueue()
        {
            RequestFile requestFile;
            this.currentLoadingFile = this.loadQueue.Dequeue();
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
                ITextureFileFormat fileFormat = TextureFileFormatUtility.GetTextureFileFormatObject(this.fileReadBuffer);
                fileFormat.LoadHeader(this.fileReadBuffer);

                if (!this.compressedTexturePacker.CanAppendTextureData(this.actualGridWidth, this.actualGridHeight))
                {
                    RemoveOldSprite();
                }
                var rect = this.compressedTexturePacker.AppendTextureData(fileFormat.width, fileFormat.height,
                    fileFormat.GeImageDataWithoutMipmap(this.fileReadBuffer));
                this.compressedTexturePacker.ApplyToTexture();
                var sprite = Sprite.Create(this.compressedTexturePacker.texture2D, rect, new Vector2(0.5f, 0.5f));
                if (this.requestedFiles.TryGetValue(currentLoadingFile, out var file))
                {
                    file.sprite = sprite;
                    this.requestedFiles[currentLoadingFile] = file;
                }
                this.currentLoadingFile = "";
                this.state = EState.None;
            }
            else if (readHandle.Status == ReadStatus.Failed || readHandle.Status == ReadStatus.Canceled || readHandle.Status == ReadStatus.Truncated)
            {
                this.state = EState.None;
            }
        }

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
            /*
            foreach(var kvs in requestedFiles.Keys)
            {
                var key = kvs;
                var val = this.requestedFiles[key];
                val.orderValue = val.orderValue - minimumOrderValue;
                requestedFiles[key] = val;
            }
            */
        }

        private void RemoveOldSprite()
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
            }
            SortOutOrderNumber();
        }

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
        }
    }
}