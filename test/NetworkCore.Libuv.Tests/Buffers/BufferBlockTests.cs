namespace NetworkCore.Libuv.Tests.Buffers
{
    using System;
    using NetworkCore.Libuv.Buffers;
    using Xunit;

    public sealed class BufferBlockTests : IDisposable
    {
        BufferBlock block;

        [Fact]
        public void Default()
        {
            this.block = new BufferBlock();
            Assert.Equal(0, this.block.Count);

            this.block.Prepare();
            Assert.Equal(this.block.Count, BufferBlock.DefaultInitial);
        }

        [Fact]
        public void Minimum()
        {
            this.block = new BufferBlock(
                BufferBlock.DefaultMinimum, 
                BufferBlock.DefaultMaximum, 
                BufferBlock.DefaultMaximum);

            const int Actual = BufferBlock.DefaultMinimum - 1;

            int count = BufferBlock.DefaultMaximum;
            while (count > BufferBlock.DefaultMinimum)
            {
                this.block.Prepare();
                Assert.Equal(count, this.block.Count);

                IRange range = this.block.Range(Actual);
                Assert.NotNull(range);
                Assert.Equal(Actual, range.Count);

                count = count > 512 ? count >> 1 : count - 16;
            }
        }

        [Fact]
        public void Maximum()
        {
            this.block = new BufferBlock(
                BufferBlock.DefaultMinimum, 
                BufferBlock.DefaultMinimum, 
                BufferBlock.DefaultMaximum);

            int count = BufferBlock.DefaultMinimum;
            while (count < BufferBlock.DefaultMaximum)
            {
                this.block.Prepare();
                Assert.Equal(count, this.block.Count);

                IRange range = this.block.Range(count);
                Assert.NotNull(range);
                Assert.Equal(count, range.Count);

                count = count >= 512 ? count << 1 : count + 16;
            }
        }

        [Fact]
        public void DisposedShouldThrow()
        {
            this.block = new BufferBlock();
            this.block.Dispose();
            Assert.Equal(0, this.block.Count);

            Assert.Throws<ObjectDisposedException>(() => this.block.Prepare());
            Assert.Throws<ObjectDisposedException>(() => this.block.Range(1024));
        }

        public void Dispose() => this.block?.Dispose();
    }
}
