namespace NetworkCore.Libuv.Buffers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    sealed class BlockRange : IRange, IDisposable
    {
        BufferBlock block;

        public BlockRange(BufferBlock block, int count)
        {
            Contract.Requires(block != null);
            Contract.Requires(count > 0);

            this.block = block;
            this.Count = count;
        }

        public IEnumerator<byte> GetEnumerator() => new RangeEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count { get; private set; }

        public static explicit operator IntPtr(BlockRange blockRange)
        {
            Contract.Requires(blockRange != null);

            return blockRange.block.AsPointer();
        }

        public byte this[int index]
        {
            get
            {
                if (index < 0
                    || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        $"Index {index} must be greater than zero and less than {this.Count - 1}");
                }

                return this.block.Array[index];
            }
        }

        public byte[] Copy()
        {
            if (this.Count == 0)
            {
                return null;
            }

            var array = new byte[this.Count];
            if (this.Count > 12)
            {
                Buffer.BlockCopy(this.block.Array, 0, array, 0, this.Count);
            }
            else
            {
                for (int index = 0; index < this.Count; ++index)
                    array[index] = this.block.Array[index];
            }

            return array;
        }

        sealed class RangeEnumerator : IEnumerator<byte>
        {
            readonly BlockRange range;
            readonly int start;
            readonly int end;
            int current;

            internal RangeEnumerator(BlockRange range)
            {
                Contract.Requires(range.block != null);
                Contract.Requires(range.Count >= 0);

                this.range = range;
                this.start = 0;
                this.end = this.start + range.Count;
                this.current = this.start - 1;
            }

            public bool MoveNext()
            {
                if (this.current < this.end)
                {
                    this.current++;
                    return (this.current < this.end);
                }

                return false;
            }

            public byte Current
            {
                get
                {
                    if (this.current < this.start)
                    {
                        throw new InvalidOperationException("Range enumeration not initialized.");
                    }

                    if (this.current >= this.end)
                    {
                        throw new InvalidOperationException("Range enumeration has already finished.");
                    }

                    return this.range[this.current];
                }
            }

            object IEnumerator.Current => this.Current;

            void IEnumerator.Reset() => this.current = this.start - 1;

            public void Dispose()
            { }
        }

        public void Dispose()
        {
            this.block = null;
            this.Count = 0;
        } 
    }
}
