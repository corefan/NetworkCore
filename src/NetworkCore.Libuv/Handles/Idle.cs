namespace NetworkCore.Libuv.Handles
{
    using System;
    using NetworkCore.Libuv.Native;

    /// <summary>
    /// Idle handles will run the given callback once per loop iteration, 
    /// right before the uv_prepare_t handles
    /// </summary>
    public sealed class Idle : WorkHandle
    {
        internal Idle(LoopContext loop)
            : base(loop, uv_handle_type.UV_IDLE)
        { }

        public void Start(Action<Idle> callback) => 
            this.ScheduleStart(state => callback.Invoke((Idle)state));
    }
}
