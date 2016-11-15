namespace NetworkCore.Libuv.Tests
{
    using System;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Requests;
    using Xunit;

    public sealed class LoopActiveTests : IDisposable
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
        public void IsAlive()
        {
            this.loop = new Loop(); // New loop should not be alive
            Assert.False(this.loop.IsAlive);

            // loops with handles are alive
            Timer timer = this.loop.CreateTimer();
            timer.Start(this.TimerCallback, 100, 0);
            Assert.True(this.loop.IsAlive);

            // loop run should not be alive
            this.loop.RunDefault();
            Assert.Equal(1, this.timerCalled); // Timer should fire
            Assert.False(this.loop.IsAlive);

            // loops with requests are alive
            bool workCallbackFired = false;
            bool afterWorkCallbackFired = false;
            Work request = this.loop.Queue(x => workCallbackFired = true, x => afterWorkCallbackFired = true);
            Assert.NotNull(request);
            Assert.True(this.loop.IsAlive);

            this.loop.RunDefault();
            Assert.True(workCallbackFired, "Work callback should be invoked.");
            Assert.True(afterWorkCallbackFired, "After work callback should be invoked");
            Assert.False(this.loop.IsAlive);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        } 
    }
}
