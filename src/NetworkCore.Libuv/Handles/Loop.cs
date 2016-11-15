namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Native;
    using NetworkCore.Libuv.Requests;

    public sealed class Loop : IDisposable
    {
        readonly LoopContext handle;

        public Loop() 
        {
            this.handle = new LoopContext();
        }

        public bool IsAlive => this.handle.IsAlive;

        public long Now => this.handle.Now;

        public long NowInHighResolution => this.handle.NowInHighResolution;

        public int ActiveHandleCount() => this.handle.ActiveHandleCount();

        public void UpdateTime() => this.handle.UpdateTime();

        internal int GetBackendTimeout() => this.handle.GetBackendTimeout();

        public int RunDefault() => this.handle.Run(uv_run_mode.UV_RUN_DEFAULT);

        public int RunOnce() => this.handle.Run(uv_run_mode.UV_RUN_ONCE);

        public int RunNoWait() => this.handle.Run(uv_run_mode.UV_RUN_NOWAIT);

        public void Stop() => this.handle.Stop();

        public Tcp CreateTcp()
        {
            this.handle.Validate();
            return new Tcp(this.handle);
        }

        public Timer CreateTimer()
        {
            this.handle.Validate();
            return new Timer(this.handle);
        }

        public Prepare CreatePrepare()
        {
            this.handle.Validate();
            return new Prepare(this.handle);
        }

        public Check CreateCheck()
        {
            this.handle.Validate();
            return new Check(this.handle);
        }

        public Idle CreateIdle()
        {
            this.handle.Validate();
            return new Idle(this.handle);
        }

        public Async CreateAsync(Action<Async> callback)
        {
            Contract.Requires(callback != null);

            this.handle.Validate();
            return new Async(this.handle, callback);
        }

        public Work Queue(Action<Work> workCallback, Action<Work> afterWorkCallback)
        {
            Contract.Requires(workCallback != null);

            this.handle.Validate();
            return new Work(this.handle, workCallback, afterWorkCallback);
        }

        public void Dispose() => this.handle.Dispose();
    }
}
