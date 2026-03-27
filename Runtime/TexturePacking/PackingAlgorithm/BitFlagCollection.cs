using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;

namespace UTJ.RuntimeCompressedTexturePacker.Packing
{
    /// <summary>
    /// Bit単位でフラグを管理します。
    /// デフォルトは全てFalse
    /// </summary>
    public struct BitFlagCollection : IDisposable
    {
        /// <summary>
        /// フラグの数
        /// </summary>
        public int Count
        {
            get;private set;
        }
        /// <summary>
        /// 実際のデータが作成されているか返します
        /// </summary>
        public bool IsDataCreated
        {
            get
            {
                return datas.IsCreated;
            }
        }
        /// 実際のデータ
        private NativeArray<byte> datas;


        /// <summary>
        /// コンストラクタ（DefaultはFalse、範囲外はtrueにしておきます)
        /// </summary>
        /// <param name="count">フラグの数</param>
        public BitFlagCollection(int count)
        {
            this.Count = count;
            this.datas = new NativeArray<byte>( (count +7 ) / 8 ,Allocator.Persistent);
            this.Clear();
        }

        /// <summary>
        /// フラグのクリア (全てFalse）
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < this.datas.Length; i++)
            {
                this.datas[i] = 0;
            }
            int tailIdx = this.datas.Length - 1;
            for (int i = 0; i < this.Count % 8; ++i)
            {
                this.datas[tailIdx] = SetFlag(this.datas[tailIdx], i, true);
            }
        }

        /// <summary>
        /// Falseにいなっている部分を探します
        /// </summary>
        /// <returns></returns>
        public int FindFalseIndex()
        {
            string str = "";
            int length = datas.Length;
            for(int i = 0; i < length; ++i)
            {
                str += datas[i] + "::";
                if (datas[i] != 0xff)
                {
                    int idx = GetFalseFlagIndex(datas[i]);
                    if(idx >= 0)
                    {
                        int val = idx + i * 8;
                        if( val  < this.Count)
                        {
                            return val;
                        }
                    }
                }
            }
            return -1;
        }
        /// <summary>
        /// フラグの設定
        /// </summary>
        /// <param name="index">何番目か指定</param>
        /// <param name="flag">フラグの値を指定</param>
        public void SetFlag(int index,bool flag)
        {
            int dataIdx = (index >> 3);
            this.datas[dataIdx] = SetFlag(datas[dataIdx], index, flag);
        }

        /// <summary>
        /// Dispose 処理
        /// </summary>
        public void Dispose()
        {
            if (this.datas.IsCreated)
            {
                this.datas.Dispose();
            }
            this.Count = 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetFalseFlagIndex(byte val)
        {
            for(int i = 0; i < 8; ++i)
            {
                if( (val & (1 <<i)) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte SetFlag(byte val , int idx,bool flag)
        {
            idx = idx & 0x07;
            if (flag)
            {
                return (byte)(val | (1 << idx));
            }
            else
            {
                return (byte)(val & (~(1 << idx)));
            }
        }

    }
}