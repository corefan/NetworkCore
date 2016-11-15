namespace NetworkCore.Libuv.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using NetworkCore.Libuv.Logging;
    using NetworkCore.Libuv.Native;

    sealed class WriteTask : IDisposable
    {
        static readonly ILog Log = LogFactory.ForContext<WriteTask>();

        Action<WriteTask, Exception> taskCallback;
        uv_buf_t buf;
        GCHandle handle;

        public WriteTask(Action<WriteTask, Exception> taskCallback)
        {
            Contract.Requires(taskCallback != null);

            this.taskCallback = taskCallback;
            int bufferSize = Marshal.SizeOf<uv_buf_t>();

            this.Request = new WatcherRequest(
                uv_req_type.UV_WRITE, 
                this.OnWriteCallback, 
                bufferSize);

            this.buf = new uv_buf_t(IntPtr.Zero, 0);
        }

        internal Action<Exception> CompletionAction { get; set; }

        internal WatcherRequest Request { get; }

        internal IntPtr InternalHandle => this.Request.InternalHandle;

        internal unsafe void Prepare(byte[] array, int offset, int count)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            this.handle = GCHandle.Alloc(this.buf, GCHandleType.Pinned);
            this.buf.Memory = (IntPtr)Unsafe.AsPointer(ref array[offset]);
            this.buf.Length = count;
        }

        public override int GetHashCode() => 
            this.Request.InternalHandle.ToInt32();

        internal uv_buf_t Buffer => this.buf;

        internal void Release()
        {
            if (this.handle.IsAllocated)
            {
                this.handle.Free();
            }

            this.buf.Memory = IntPtr.Zero;
            this.buf.Length = 0;
        }

        void OnWriteCallback(WatcherRequest request, Exception error)
        {
            Log.Trace($"{nameof(WriteTask)} Write request callback, releasing resources.");

            try
            {
                // Free all resource before it can be recycled
                this.Release();
                this.taskCallback?.Invoke(this, error);
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(WriteTask)} Write request callback error.", exception);

                // If exception is thrown, we cannot recyled this request
                this.Dispose();
            }
        }

        public void Dispose()
        {
            this.Release();

            this.Request.Dispose();
            this.taskCallback = null;
            this.CompletionAction = null;
        } 
    }
}
