using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using System;

namespace UTJ.RuntimeCompressedTexturePacker
{
    /// <summary>
    /// AsyncReadManagerを用いたUnsafeのファイルリード周りのUtility
    /// </summary>
    public static class UnsafeFileReadUtility
    {
        /// <summary>
        /// ファイルサイズの取得
        /// </summary>
        /// <param name="path">パス指定</param>
        /// <returns>ファイルサイズを返します</returns>
        public static unsafe long GetFileSize(string path)
        {
            FileInfoResult result;
            var info = AsyncReadManager.GetFileInfo(path, &result);
            // Asyncの処理を即時で終了させます
            info.JobHandle.Complete();
            if (result.FileState == FileState.Exists)
            {
                return result.FileSize;
            }
            return -1;
        }


        /// <summary>
        /// ファイルロードリクエストの発行
        /// </summary>
        /// <param name="path">パス指定</param>
        /// <param name="buffer">バッファ指定</param>
        /// <param name="fileSize">ファイルサイズ指定</param>
        /// <returns></returns>
        public static unsafe ReadHandle RequestLoad(string path, NativeArray<byte> buffer, long fileSize)
        {
            ReadCommand readCommand = new ReadCommand()
            {
                Buffer = buffer.GetUnsafePtr(),
                Offset = 0,
                Size = fileSize
            };
            var handle = AsyncReadManager.Read(path, &readCommand, 1);
            return handle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static NativeArray<byte> LoadFileSync(string path,Allocator allocator)
        {
            var fileSize = GetFileSize(path);
            if(fileSize <= 0)
            {
                return new NativeArray<byte>();
            }
            var fileBinary = new NativeArray<byte>( (int)fileSize,allocator);
            var handle = RequestLoad(path, fileBinary, fileSize);
            handle.JobHandle.Complete();
            return fileBinary;
        }

    }
}