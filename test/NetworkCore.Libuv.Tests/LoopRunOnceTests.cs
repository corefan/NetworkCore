namespace NetworkCore.Libuv.Tests
{
    using System;
    using NetworkCore.Libuv.Handles;
    using Xunit;

    public sealed class LoopRunOnceTests : IDisposable
    {
        const int NumberOfTicks = 64;

        Loop loop;
        int idleCounter;

        void IdleCallback(Idle handle)
        {
            if (handle != null)
            {
                this.idleCounter++;

                if (this.idleCounter == NumberOfTicks)
                {
                    handle.Stop();
                }
            }
        }

        [Fact]
        public void Once()
        {
            this.loop = new Loop();

            Idle idle = this.loop.CreateIdle();
            idle.Start(this.IdleCallback);

            while (this.loop.RunOnce() != 0)
            {
                Assert.True(idle.IsValid);
            }
                

            Assert.Equal(NumberOfTicks, this.idleCounter);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
