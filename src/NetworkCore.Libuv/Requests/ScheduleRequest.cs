namespace NetworkCore.Libuv.Requests
{
    using System;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Logging;
    using NetworkCore.Libuv.Native;

    public abstract class ScheduleRequest : IDisposable
    {
        internal static readonly ILog Log = LogFactory.ForContext<ScheduleHandle>();

        internal ScheduleRequest(uv_req_type requestType)
        {
            this.RequestType = requestType;
        }

        public virtual bool IsValid => this.InternalHandle != IntPtr.Zero;

        internal abstract IntPtr InternalHandle { get; }

        internal uv_req_type RequestType { get; }

        public bool Cancel() => this.IsValid 
            && NativeMethods.Cancel(this.InternalHandle);

        protected abstract void Close();

        public override string ToString() =>
            $"{this.RequestType} {this.InternalHandle}";

        public void Dispose()
        {
            if (!this.IsValid)
            {
                return;
            }

            this.Close();
        }
    }
}
