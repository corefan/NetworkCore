namespace NetworkCore.Libuv.Tests
{
    using System;
    using NetworkCore.Libuv.Handles;
    using Xunit;

    public sealed class LoopStopTests : IDisposable
    {
        const int NumberOfticks = 10;
        Loop loop;

        int timerCalled;
        int prepareCalled;

        void PrepareCallback(Prepare handle)
        {
            this.prepareCalled++;
            if (this.prepareCalled == NumberOfticks)
            {
                handle.Stop();
            }
        }

        void TimerCallback(Timer handle)
        {
            this.timerCalled++;
            if (this.timerCalled == 1)
            {
                this.loop?.Stop();
            }
            else if (this.timerCalled == NumberOfticks)
            {
                handle.Stop();
            }
        }

        [Fact]
        public void Stop()
        {
            this.loop = new Loop();

            Prepare prepare = this.loop.CreatePrepare();
            prepare.Start(this.PrepareCallback);

            Timer timer = this.loop.CreateTimer();
            timer.Start(this.TimerCallback, 100, 100);

            this.loop.RunDefault();
            Assert.Equal(1, this.timerCalled);

            this.loop.RunNoWait();
            Assert.True(this.prepareCalled > 1);

            this.loop.RunDefault();
            Assert.Equal(10, this.timerCalled);
            Assert.Equal(10, this.prepareCalled);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
