namespace NetworkCore.Libuv.Tests
{
    using System;
    using NetworkCore.Libuv.Handles;
    using Xunit;

    public sealed class CallbackOrderTests : IDisposable
    {
        Loop loop;
        Idle idle;
        Timer timer;

        int idleCalled;
        int timerCalled;
        bool idleCallbackCheck;
        bool timerCallbackCheck;

        void IdleCallback(Idle handle)
        {
            this.idleCallbackCheck = (this.idleCalled == 0 && this.timerCalled == 1);
            if (handle != null 
                && handle == this.idle)
            {
                handle.Stop();
                this.idleCalled++;
            }
        }

        void TimerCallback(Timer handle)
        {
            this.timerCallbackCheck = (this.idleCalled == 0 && this.timerCalled == 0);
            if (handle != null 
                && handle == this.timer)
            {
                handle.Stop();
                this.timerCalled++;
            }
        }

        void NextTick(Idle handle)
        {
            if (handle != null)
            {
                handle.Stop();

                this.idle = this.loop.CreateIdle();
                this.idle.Start(this.IdleCallback);

                this.timer = this.loop.CreateTimer();
                this.timer.Start(this.TimerCallback, 0, 0);
            }
        }

        [Fact]
        public void Order()
        {
            this.loop = new Loop();
            this.idleCallbackCheck = false;
            this.timerCallbackCheck = false;
            this.idleCalled = 0;
            this.timerCalled = 0;

            Idle idleStart = this.loop.CreateIdle();
            idleStart.Start(this.NextTick);

            Assert.Equal(0, this.idleCalled);
            Assert.Equal(0, this.timerCalled);

            this.loop.RunDefault();

            Assert.Equal(1, this.idleCalled);
            Assert.Equal(1, this.timerCalled);

            Assert.True(this.timerCallbackCheck);
            Assert.True(this.idleCallbackCheck);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }

    }
}
