namespace NetworkCore.Libuv.Tests
{
    using System;
    using NetworkCore.Libuv.Handles;
    using Xunit;

    public sealed class ActiveTests : IDisposable
    {
        Loop loop;
        int timerCalled;

        void TimerCallback(Timer handle)
        {
            if (handle != null)
            {
                this.timerCalled++;
            }
        }

        [Fact]
        public void Active()
        {
            this.loop = new Loop();
            this.timerCalled = 0;

            Timer timer = this.loop.CreateTimer();
            Assert.NotNull(timer);

            /* uv_is_active() and uv_is_closing() should always return either 0 or 1. */
            Assert.False(timer.IsActive);
            Assert.False(timer.IsClosing);

            timer.Start(this.TimerCallback, 1000, 0);
            Assert.True(timer.IsActive);
            Assert.False(timer.IsClosing);

            timer.Stop();
            Assert.False(timer.IsActive);
            Assert.False(timer.IsClosing);

            timer.Start(this.TimerCallback, 1000, 0);
            Assert.True(timer.IsActive);
            Assert.False(timer.IsClosing);

            timer.Dispose();
            Assert.False(timer.IsActive);
            Assert.True(timer.IsClosing);

            int result = this.loop.RunDefault();
            Assert.Equal(0, result);
            Assert.False(timer.IsValid);
            Assert.Equal(0, this.timerCalled);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
