namespace NetworkCore.Libuv.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetworkCore.Libuv.Native;

    sealed unsafe class RequestContext : NativeHandle
    {
        readonly uv_req_type requestType;

        internal RequestContext(
            uv_req_type requestType,
            int size,
            ScheduleRequest target)
        {
            Contract.Requires(size >= 0);
            Contract.Requires(target != null);

            int totalSize = NativeMethods.GetSize(requestType);
            totalSize += size;
            IntPtr handle = Marshal.AllocHGlobal(totalSize);

            GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
            *(IntPtr*)handle = GCHandle.ToIntPtr(gcHandle);

            this.Handle = handle;
            this.requestType = requestType;

            Log.Debug($"{requestType} {handle} allocated.");
        }

        internal RequestContext(
            uv_req_type requestType,
            Action<IntPtr> initializer,
            ScheduleRequest target)
        {
            Contract.Requires(initializer != null);
            Contract.Requires(target != null);

            int size = NativeMethods.GetSize(requestType);
            IntPtr handle = Marshal.AllocHGlobal(size);

            initializer(handle);

            GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
            ((uv_req_t*)handle)->data = GCHandle.ToIntPtr(gcHandle);

            this.Handle = handle;
            this.requestType = requestType;

            Log.Debug($"{requestType} {handle} allocated.");
        }

        internal static T GetTarget<T>(IntPtr handle)
        {
            try
            {
                IntPtr pHandle = ((uv_req_t*)handle)->data;
                GCHandle gcHandle = GCHandle.FromIntPtr(pHandle);

                return (T)gcHandle.Target;
            }
            catch (InvalidOperationException exception)
            {
                Log.Error($"GCHandle for {handle} is not valid.", exception);
                return default(T);
            }
        }

        protected internal override void CloseHandle()
        {
            IntPtr handle = this.Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            IntPtr pHandle = ((uv_req_t*)handle)->data;

            // Free GCHandle
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    nativeHandle.Free();
                    ((uv_req_t*)handle)->data = IntPtr.Zero;
                    Log.Debug($"{this.requestType} {handle} GCHandle released.");
                }
            }

            // Release memory
            Marshal.FreeHGlobal(handle);
            this.Handle = IntPtr.Zero;
            Log.Debug($"{this.requestType} {handle} memory released.");
        }
    }
}
