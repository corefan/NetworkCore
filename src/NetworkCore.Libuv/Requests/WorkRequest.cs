namespace NetworkCore.Libuv.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Native;

    public sealed class WorkRequest : ScheduleRequest
    {
        internal static readonly uv_work_cb WorkCallback = OnWorkCallback;

        readonly RequestContext handle;
        Action<WorkRequest> workCallback;

        internal WorkRequest(
            uv_req_type requestType, 
            Action<WorkRequest> workCallback,
            Action<IntPtr> initializer)
            : base(requestType)
        {
            Contract.Requires(workCallback != null);
            Contract.Requires(initializer != null);

            this.workCallback = workCallback;
            this.handle = new RequestContext(requestType, initializer, this);
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

            var request = RequestContext.GetTarget<WorkRequest>(handle);
            request?.OnWorkCallback();
        }

        protected override void Close()
        {
            this.workCallback = null;
            this.handle.Dispose();
        } 
    }
}
