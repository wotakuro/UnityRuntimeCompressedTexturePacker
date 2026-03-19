using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;


namespace UTJ.RuntimeCompressedTexturePacker
{
    /// <summary>
    /// AsyncReadManagerを用いたUnsafeのファイルリード周りのUtility
    /// </summary>
    public static class BytesToOtherTypesUtility
    {
        /// <summary>
        /// ASTC向けに3Byte読み込みをします
        /// </summary>
        /// <param name="bytes">データ</param>
        /// <param name="index">読み込み開始位置</param>
        /// <returns>3Byte読んだ値</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadData3BytesForAstc(NativeArray<byte> bytes,int index)
        {
            return (uint)(bytes[index] + (bytes[index+1] << 8) + (bytes[index+2] << 16));
        }

        /// <summary>
        /// Uintをそのまま読みます。4Byte Alignしてください
        /// </summary>
        /// <param name="bytes">Byteの指定</param>
        /// <param name="index">Indexの指定</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint ReadUintFast(NativeArray<byte> bytes, int index)
        {
            byte* ptr = (byte*)bytes.GetUnsafeReadOnlyPtr() + index;
#if DEBUG
            if (((nuint)ptr & 3) != 0)
            {
                UnityEngine.Debug.LogWarning("ReadUintFast should be 4 bytes align");
            }
#endif
            var uintPtr = (uint*)ptr;
            return *uintPtr;
        }

        /// <summary>
        /// UintでEndianをスワップします
        /// </summary>
        /// <param name="val">値</param>
        /// <returns>スワップ後のエンディアン</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SwapUintEndian(uint val)
        {
            return (val & 0x000000FFU) << 24 |
                (val & 0x0000FF00U) << 8 |           
                (val & 0x00FF0000U) >> 8 |
                (val & 0xFF000000U) >> 24;
        }


        /// <summary>
        /// 実行環境がLitteEndianかチェックします
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsLittleEndianRuntime()
        {
            int value = 1;
            byte* ptr = (byte*)&value;
            return *ptr == 1;
        }


        /// <summary>
        /// 4Byteアラインされているかチェックして返します
        /// </summary>
        /// <param name="ptr">Pointer指定</param>
        /// <returns>4バイトアラインされているならTrueを返します</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool Is4ByteAlign(void* ptr)
        {
            return (((nuint)ptr & 3) == 0);
        }

        /// <summary>
        /// 8Byteアラインされているかチェックして返します
        /// </summary>
        /// <param name="ptr">Pointer指定</param>
        /// <returns>8バイトアラインされているならTrueを返します</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool Is8ByteAlign(void* ptr)
        {
            return (((nuint)ptr & 7) == 0);
        }
    }
}