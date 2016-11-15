namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetworkCore.Libuv.Native;

    sealed unsafe class HandleContext : NativeHandle
    {
        static readonly uv_close_cb CloseCallback = OnCloseHandle;
        readonly uv_handle_type handleType;

        internal HandleContext(
            uv_handle_type handleType, 
            Action<IntPtr> initializer, 
            ScheduleHandle target)
        {
            Contract.Requires(initializer != null);
            Contract.Requires(target != null);

            int size = NativeMethods.GetSize(handleType);
            IntPtr handle = Marshal.AllocHGlobal(size);

            initializer(handle);

            GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
            ((uv_handle_t*)handle)->data = GCHandle.ToIntPtr(gcHandle);

            this.Handle = handle;
            this.handleType = handleType;

            Log.Info($"{handleType} {handle} allocated.");
        }

        internal bool IsActive => this.IsValid 
            && NativeMethods.IsHandleActive(this.Handle);

        internal bool IsClosing => this.IsValid 
            && NativeMethods.IsHandleClosing(this.Handle);

        internal void AddReference()
        {
            this.Validate();
            NativeMethods.AddReference(this.Handle);
        }

        internal void ReleaseReference()
        {
            this.Validate();
            NativeMethods.ReleaseReference(this.Handle);
        }

        protected internal override void CloseHandle()
        {
            IntPtr handle = this.Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.CloseHandle(handle, CloseCallback);
            Log.Info($"{this.handleType} {this.Handle} closed, releasing resources pending.");
        }

        internal static T GetTarget<T>(IntPtr handle)
        {
            IntPtr inernalHandle = ((uv_handle_t*)handle)->data;
            GCHandle gcHandle = GCHandle.FromIntPtr(inernalHandle);
            return (T)gcHandle.Target;
        }

        static void OnCloseHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            ScheduleHandle scheduleHandle = null;

            // Get gc handle first
            IntPtr pHandle = ((uv_handle_t*)handle)->data;
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    scheduleHandle = nativeHandle.Target as ScheduleHandle;
                    nativeHandle.Free();

                    ((uv_handle_t*)handle)->data = IntPtr.Zero;
                    Log.Trace($"{scheduleHandle?.GetType()} {handle} GCHandle released.");
                }
            }

            // Release memory
            Marshal.FreeHGlobal(handle);
            scheduleHandle?.SetHandleAsInvalid();

            Log.Info($"{handle} memory and GCHandle released.");
        }
    }
}
