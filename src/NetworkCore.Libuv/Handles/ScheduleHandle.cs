namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Logging;
    using NetworkCore.Libuv.Native;

    public abstract class ScheduleHandle : IDisposable
    {
        protected static readonly ILog Log = LogFactory.ForContext<ScheduleHandle>();

        readonly HandleContext handle;

        internal ScheduleHandle(LoopContext loop, uv_handle_type handleType)
        {
            Contract.Requires(loop != null);

            HandleContext initialHandle = NativeMethods.Initialize(loop.Handle, handleType, this);
            if (initialHandle == null)
            {
                throw new InvalidOperationException($"Initialize {handleType} for loop {loop.Handle} failed.");
            }

            this.handle = initialHandle;
            this.HandleType = handleType;
        }

        public bool IsActive => this.handle.IsActive;

        public bool IsClosing => this.handle.IsClosing;

        internal IntPtr InternalHandle => this.handle.Handle;

        public bool IsValid => this.handle.IsValid;

        internal uv_handle_type HandleType { get; }

        internal void SetHandleAsInvalid() => this.handle.SetHandleAsInvalid();

        internal void Validate() => this.handle.Validate();

        public unsafe bool TryGetLoop(out Loop loop)
        {
            loop = null;
            try
            {
                IntPtr nativeHandle = this.InternalHandle;
                if (nativeHandle == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr loopHandle = ((uv_handle_t*)nativeHandle)->loop;
                if (loopHandle != IntPtr.Zero)
                {
                    loop = HandleContext.GetTarget<Loop>(loopHandle);
                }

                return loop != null;
            }
            catch (Exception exception)
            {
                Log.Warn($"{this.HandleType} Failed to get loop.", exception);
                return false;
            }
        }

        public void CloseHandle()
        {
            try
            {
                if (!this.IsValid)
                {
                    return;
                }

                this.Close();
                this.handle.Dispose();
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(ScheduleHandle)} {this.HandleType} Failed to close handle.", exception);
                throw;
            }
        }

        protected abstract void Close();

        public void Stop()
        {
            if (!this.IsValid)
            {
                return;
            }

            NativeMethods.Stop(this.HandleType, this.handle.Handle);
        }

        public void AddReference()
        {
            if (!this.IsValid)
            {
                return;
            }

            this.handle.AddReference();
        }

        public void RemoveReference()
        {
            if (!this.IsValid)
            {
                return;
            }

            this.handle.ReleaseReference();
        }

        public void Dispose()
        {
            try
            {
                this.CloseHandle();
            }
            catch (Exception exception)
            {
                Log.Warn($"{this.handle} Failed to close and releasing resources.", exception);
            }
        }
    }
}
