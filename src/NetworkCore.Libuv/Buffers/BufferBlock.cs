namespace NetworkCore.Libuv.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using NetworkCore.Libuv.Common;
    using NetworkCore.Libuv.Logging;

    sealed class BufferBlock : IDisposable
    {
        static readonly ILog Log = LogFactory.ForContext<BufferBlock>();

        static readonly byte[] Empty = new byte[0];
        static readonly int[] SizeTable;

        internal const int DefaultMinimum = 64;
        internal const int DefaultInitial = 1024;
        internal const int DefaultMaximum = 1024 * 8;

        internal const int IndexIncrement = 1;
        internal const int IndexDecrement = 1;

        readonly int minmun;
        readonly int maximum;

        GCHandle gcHandle;
        int index;
        int nextSize;

        public BufferBlock() 
            : this(DefaultMinimum, DefaultInitial, DefaultMaximum)
        { }

        public BufferBlock(int minimum, int initial, int maximum)
        {
            Contract.Requires(minimum > 0);
            Contract.Requires(initial >= minimum);
            Contract.Requires(maximum >= initial);

            int min = GetSizeTableIndex(minimum);
            if (SizeTable[min] < minimum)
            {
                this.minmun = min + 1;
            }
            else
            {
                this.minmun = min;
            }

            int max = GetSizeTableIndex(maximum);
            if (SizeTable[max] > maximum)
            {
                this.maximum = max - 1;
            }
            else
            {
                this.maximum = max;
            }

            this.Array = Empty;
            this.index = GetSizeTableIndex(initial);
            this.nextSize = SizeTable[this.index];
        }

        internal int Count => this.Array?.Length ?? 0;

        internal byte[] Array { get; private set; }

        internal unsafe IntPtr AsPointer()
        {
            if (this.Array == null)
            {
                throw new ObjectDisposedException($"{nameof(BufferBlock)}");
            }

            return (IntPtr)Unsafe.AsPointer(ref this.Array[0]);
        }

        internal void Prepare()
        {
            if (this.Array == null)
            {
                throw new ObjectDisposedException($"{nameof(BufferBlock)}");
            }

            if (this.Array.Length != 0 
                && this.Array.Length == this.nextSize)
            {
                return;
            }

            Log.Debug($"{nameof(BufferBlock)} Buffer size changed from {this.Array.Length} to {this.nextSize}.");
            this.Renew(new byte[this.nextSize]);
        }

        internal BlockRange Range(int actualSize)
        {
            if (this.Array == null)
            {
                throw new ObjectDisposedException($"{nameof(BufferBlock)}");
            }

            if (actualSize <= SizeTable[Math.Max(0, this.index - IndexDecrement - 1)])
            {
                this.index = Math.Max(this.index - IndexDecrement, this.minmun);
                this.nextSize = SizeTable[this.index];
            }
            else if (actualSize >= this.nextSize)
            {
                this.index = Math.Min(this.index + IndexIncrement, this.maximum);
                this.nextSize = SizeTable[this.index];
            }

            return new BlockRange(this, actualSize);
        }

        void Renew(byte[] newValue)
        {
            if (this.gcHandle.IsAllocated)
            {
                this.gcHandle.Free();
            }

            this.Array = newValue;
            if (this.Array != null 
                || this.Array != Empty)
            {
                this.gcHandle = GCHandle.Alloc(this.Array, GCHandleType.Pinned);
            }
        }

        public void Dispose() => this.Renew(null);

        static BufferBlock()
        {
            var sizeTable = new List<int>();
            for (int i = 16; i < 512; i += 16)
            {
                sizeTable.Add(i);
            }

            for (int i = 512; i > 0; i <<= 1)
            {
                sizeTable.Add(i);
            }

            SizeTable = sizeTable.ToArray();
        }

        static int GetSizeTableIndex(int size)
        {
            for (int low = 0, high = SizeTable.Length - 1;;)
            {
                if (high < low)
                {
                    return low;
                }
                if (high == low)
                {
                    return high;
                }

                int mid = (low + high).RightUShift(1);
                int a = SizeTable[mid];
                int b = SizeTable[mid + 1];
                if (size > b)
                {
                    low = mid + 1;
                }
                else if (size < a)
                {
                    high = mid - 1;
                }
                else if (size == a)
                {
                    return mid;
                }
                else
                {
                    return mid + 1;
                }
            }
        }
    }
}
