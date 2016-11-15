namespace NetworkCore.Libuv.Handles
{
    using System;
    using NetworkCore.Libuv.Native;

    /// <summary>
    /// Prepare handles will run the given callback once per loop iteration, 
    /// right before polling for i/o.
    /// </summary>
    public sealed class Prepare : WorkHandle
    {
        internal Prepare(LoopContext loop)
            : base(loop, uv_handle_type.UV_PREPARE)
        { }

        public void Start(Action<Prepare> callback) => 
            this.ScheduleStart(state => callback.Invoke((Prepare)state));
    }
}
