namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Native;

    public class WorkHandle : ScheduleHandle
    {
        internal static readonly uv_work_cb WorkCallback = OnWorkCallback;
        protected Action<WorkHandle> Callback;

        internal WorkHandle(LoopContext loop, uv_handle_type handleType)
            : base(loop, handleType)
        { }

        protected void ScheduleStart(Action<WorkHandle> callback)
        {
            Contract.Requires(callback != null);

            this.Validate();
            this.Callback = callback;
            NativeMethods.Start(this.HandleType, this.InternalHandle, WorkCallback);
        }

        protected override void Close() => this.Callback = null;

        void OnWorkCallback()
        {
            try
            {
                this.Callback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} {this.InternalHandle} callback error.", exception);
                throw;
            }
        }

        static void OnWorkCallback(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var workHandle = HandleContext.GetTarget<WorkHandle>(handle);
            workHandle?.OnWorkCallback();
        }
    }
}
