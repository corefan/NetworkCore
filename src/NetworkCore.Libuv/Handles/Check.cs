namespace NetworkCore.Libuv.Handles
{
    using System;
    using NetworkCore.Libuv.Native;

    /// <summary>
    /// Check handles will run the given callback once per loop iteration, 
    /// right after polling for i/o.
    /// </summary>
    public sealed class Check : WorkHandle
    {
        internal Check(LoopContext loop)
            : base(loop, uv_handle_type.UV_CHECK)
        { }

        public void Start(Action<Check> callback) => 
            this.ScheduleStart(state => callback.Invoke((Check)state));
    }
}
