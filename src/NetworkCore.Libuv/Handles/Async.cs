namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Native;

    /// <summary>
    /// Async handles allow the user to “wakeup” the event loop and get 
    /// a callback called from another thread.
    /// </summary>
    public sealed class Async : WorkHandle
    {
        internal Async(LoopContext loop, Action<Async> callback)
            : base(loop, uv_handle_type.UV_ASYNC)
        {
            Contract.Requires(callback != null);

            this.Callback = state => callback.Invoke((Async)state);
        }

        public void Send()
        {
            this.Validate();
            NativeMethods.Send(this.InternalHandle);
        }
    }
}
