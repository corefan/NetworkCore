namespace NetworkCore.Libuv.Tests
{
    using System;
    using NetworkCore.Libuv.Handles;
    using Xunit;

    public sealed class LoopRunNoWaitTests : IDisposable
    {
        Loop loop;
        int timerCalled;

        void TimerCallback(Timer handle) => this.timerCalled++;

        [Fact]
        public void NoWait()
        {
            this.loop = new Loop();

            Timer timer = this.loop.CreateTimer();
            timer.Start(this.TimerCallback, 100, 100);

            int result = this.loop.RunNoWait();
            Assert.True(result != 0, "Loop run nowait should return non zero.");
            Assert.Equal(0, this.timerCalled);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
