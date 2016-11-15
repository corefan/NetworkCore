namespace NetworkCore.Libuv.Tests.Performance
{
    using System;
    using NetworkCore.Libuv.Handles;

    sealed class LoopCount : IDisposable
    {
        const long NanoSeconds = 1000000000;
        const long NumberOfTicks = (2 * 1000 * 1000);

        Idle idle;
        long ticks;

        public void Run()
        {
            this.ticks = 0;
            this.RunCount();
            this.ticks = 0;
            this.RunTimed();
        }

        void RunCount()
        {
            var loop = new Loop();

            this.idle = loop.CreateIdle();
            this.idle.Start(this.OnIdleTickCallback);

            long start = loop.NowInHighResolution;
            loop.RunDefault();
            long stop = loop.NowInHighResolution;
            long duration = stop - start;
            double seconds = (double)duration / NanoSeconds;
            long ticksPerSecond = (long)Math.Floor(NumberOfTicks / seconds);
            Console.WriteLine($"Loop count : {NumberOfTicks} ticks in {seconds} seconds ({ticksPerSecond}/s).");
            
            this.idle.Dispose();
            loop.Dispose();
        }

        void OnIdleTickCallback(Idle handle)
        {
            this.ticks++;
            if (this.ticks >= NumberOfTicks)
            {
                handle.Stop();
            }
        }

        void RunTimed()
        {
            var loop = new Loop();

            this.idle = loop.CreateIdle();
            this.idle.Start(this.OnIdleTimedCallback);

            Timer timer = loop.CreateTimer();
            timer.Start(this.OnTimerCallback, 5000, 0);

            loop.RunDefault();
            Console.WriteLine($"Loop count timed : {this.ticks} ticks ({this.ticks / 0.5} ticks/s).");

            this.idle.Dispose();
            loop.Dispose();
        }

        void OnTimerCallback(Timer timer)
        {
            this.idle.Stop();
            timer.Stop();
        }

        void OnIdleTimedCallback(Idle handle) => this.ticks++;

        public void Dispose() => this.idle = null;
    }
}
