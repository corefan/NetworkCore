namespace NetworkCore.Libuv.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Native;

    public sealed class Work : ScheduleRequest
    {
        internal static readonly uv_work_cb WorkCallback = OnWorkCallback;
        internal static readonly uv_watcher_cb AfterWorkCallback = OnAfterWorkCallback;

        readonly RequestContext handle;

        Action<Work> workCallback;
        Action<Work> afterWorkCallback;

        internal Work(
            LoopContext loop, 
            Action<Work> workCallback, 
            Action<Work> afterWorkCallback)
            : base(uv_req_type.UV_WORK)
        {
            Contract.Requires(loop != null);
            Contract.Requires(workCallback != null);

            this.workCallback = workCallback;
            this.afterWorkCallback = afterWorkCallback;

            this.handle = new RequestContext(
                uv_req_type.UV_WORK, 
                h => NativeMethods.Queue(loop.Handle, h), 
                this);
        }

        internal override IntPtr InternalHandle => this.handle.Handle;

        void OnWorkCallback()
        {
            try
            {
                this.workCallback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.GetType()} callback error.", exception);
                throw;
            }
        }

        static void OnWorkCallback(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var request = RequestContext.GetTarget<Work>(handle);
            request?.OnWorkCallback();
        }

        void OnAfterWorkCallback()
        {
            try
            {
                this.afterWorkCallback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.GetType()} callback error", exception);
                throw;
            }
        }

        static void OnAfterWorkCallback(IntPtr handle, int status)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var request = RequestContext.GetTarget<Work>(handle);
            request?.OnAfterWorkCallback();
        }

        protected override void Close()
        {
            this.workCallback = null;
            this.afterWorkCallback = null;
            this.handle.Dispose();
        }
    }
}
