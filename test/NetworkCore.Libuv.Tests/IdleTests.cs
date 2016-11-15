namespace NetworkCore.Libuv.Tests
{
    using System;
    using NetworkCore.Libuv.Handles;
    using Xunit;

    public sealed class IdleTests : IDisposable
    {
        Loop loop;
        Idle idle;
        Check check;
        Timer timer;

        int idleCalled;
        int checkCalled;
        int timerCalled;

        void IdleCallback(Idle handle)
        {
            if (handle != null)
            {
                this.idleCalled++;
            }
        }

        void CheckCallback(Check handle)
        {
            if (handle != null 
                && handle == this.check)
            {
                this.checkCalled++;
            }
        }

        void TimerCallback(Timer handle)
        {
            if (handle != null 
                && handle == this.timer)
            {
                this.idle?.Dispose();
                this.check?.Dispose();
                this.timer?.Dispose();

                this.timerCalled++;
            }
        }

        [Fact]
        public void IdleStarvation()
        {
            this.loop = new Loop();

            this.idle = this.loop.CreateIdle();
            this.idle.Start(this.IdleCallback);

            this.check = this.loop.CreateCheck();
            this.check.Start(this.CheckCallback);

            this.timer = this.loop.CreateTimer();
            this.timer.Start(this.TimerCallback, 50, 0);

            this.loop.RunDefault();

            Assert.True(this.idleCalled > 0, "Idle callback should be invoked at least once.");
            Assert.Equal(1, this.timerCalled);
            Assert.True(this.checkCalled > 0, "Check callback should be invoked at least once.");

            Assert.NotNull(this.idle);
            Assert.False(this.idle.IsValid);

            Assert.NotNull(this.check);
            Assert.False(this.check.IsValid);

            Assert.NotNull(this.timer);
            Assert.False(this.timer.IsValid);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
