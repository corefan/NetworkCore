namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Native;

    /// <summary>
    /// Timer handles are used to schedule callbacks to be called in the future.
    /// </summary>
    public sealed class Timer : WorkHandle
    {
        internal Timer(LoopContext loop)
            : base(loop, uv_handle_type.UV_TIMER)
        { }

        public void Start(Action<Timer> callback, long timeout, long repeat)
        {
            Contract.Requires(callback != null);

            this.Validate();

            this.Callback = state => callback.Invoke((Timer)state);
            NativeMethods.Start(this.InternalHandle, WorkCallback, timeout, repeat);
        }

        public void SetRepeat(long repeat)
        {
            this.Validate();
            NativeMethods.SetTimerRepeat(this.InternalHandle, repeat);
        }

        public long GetRepeat()
        {
            this.Validate();
            return NativeMethods.GetTimerRepeat(this.InternalHandle);
        }

        public void Again()
        {
            this.Validate();
            NativeMethods.Again(this.InternalHandle);
        }
    }
}
