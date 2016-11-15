namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Buffers;
    using NetworkCore.Libuv.Native;
    using NetworkCore.Libuv.Requests;

    public abstract class StreamHandle : ScheduleHandle
    {
        internal static readonly uv_watcher_cb ConnectionCallback = OnConnectionCallback;
        internal static readonly uv_alloc_cb AllocateCallback = OnAllocateCallback;
        internal static readonly uv_read_cb ReadCallback = OnReadCallback;

        Action<StreamHandle, Exception> connectionHandler;

        internal StreamHandle(
            LoopContext loop,
            uv_handle_type handleType)
            : base(loop, handleType)
        {
            this.Pipeline = new Pipeline(this);
        }

        public bool IsReadable =>
            NativeMethods.IsStreamReadable(this.InternalHandle);

        public bool IsWritable =>
            NativeMethods.IsStreamWritable(this.InternalHandle);

        internal Pipeline Pipeline { get; }

        protected internal void ShutdownStream(Action<StreamHandle, Exception> completion = null)
        {
            if (!this.IsValid
                || !this.IsActive)
            {
                return;
            }

            StreamShutdown streamShutdown = null;
            try
            {
                streamShutdown = new StreamShutdown(this, completion);
            }
            catch (Exception exception)
            {
                Exception error = exception;

                int? errorCode = (error as OperationException)?.ErrorCode;
                if (errorCode == (int)uv_err_code.UV_EPIPE)
                {
                    // It is ok if the stream is already down
                    error = null;
                }
                Log.Error($"{this.HandleType} {this.InternalHandle} failed to shutdown.", error);

                StreamShutdown.Completed(completion, this, error);
                streamShutdown?.Dispose();
            }
        }

        protected void QueueWriteStream(byte[] array, int offset, int count, Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            this.Pipeline.QueueWrite(array, offset, count, completion);
        }

        internal void WriteStream(WriteTask request)
        {
            Contract.Requires(request != null);

            this.Validate();
            try
            {
                NativeMethods.WriteStream(
                    request.InternalHandle, 
                    this.InternalHandle, 
                    request.Buffer);
            }
            catch (Exception exception)
            {
                Log.Debug($"{this.HandleType} Failed to write data.", exception);
                throw;
            }
        }

        public void TryWrite(byte[] array)
        {
            Contract.Requires(array != null && array.Length > 0);

            this.TryWrite(array, 0, array.Length);
        }

        internal unsafe void TryWrite(byte[] array, int offset, int count)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            this.Validate();
            try
            {
                fixed (byte* memory = array)
                {
                    var buf = new uv_buf_t((IntPtr)memory + offset, count);
                    NativeMethods.TryWriteStream(this.InternalHandle, buf);
                }
            }
            catch (Exception exception)
            {
                Log.Debug($"{this.HandleType} Trying to write data failed.", exception);
                throw;
            }
        }

        internal void ReadStart()
        {
            this.Validate();
            NativeMethods.StreamReadStart(this.InternalHandle);
            Log.Trace($"{this.HandleType} {this.InternalHandle} Read started.");
        }

        internal void ReadStop()
        {
            if (!this.IsValid)
            {
                return;
            }

            // This function is idempotent and may be safely called on a stopped stream.
            NativeMethods.StreamReadStop(this.InternalHandle);
            Log.Trace($"{this.HandleType} {this.InternalHandle} Read stopped.");
        }

        protected override void Close()
        {
            this.connectionHandler = null;
            this.Pipeline.Dispose();
        } 

        static void OnConnectionCallback(IntPtr handle, int status)
        {
            var server = RequestContext.GetTarget<StreamHandle>(handle);
            if (server == null)
            {
                return;
            }

            StreamHandle client = null;
            Exception error = null;
            try
            {
                if (status < 0)
                {
                    error = NativeMethods.CreateError((uv_err_code)status);
                }
                else
                {
                    client = server.NewStream();
                    NativeMethods.StreamAccept(server.InternalHandle, client.InternalHandle);
                }

                server.Accept(client, error);

                Log.Debug($"{server.GetType().Name} Connection accepted.");
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(StreamHandle)} Failed to accept client connection.", exception);
                client?.Dispose();
            }
        }

        internal void Listen(int backlog, Action<StreamHandle, Exception> handler)
        {
            Contract.Requires(backlog >= 0);
            Contract.Requires(handler != null);

            this.Validate();
            this.connectionHandler = handler;
            NativeMethods.StreamListen(this.InternalHandle, backlog);

            Log.Debug($"{this.HandleType} {this.InternalHandle} start listening. Backlog = {backlog}");
        }

        void Accept(StreamHandle client, Exception error)
        {
            Contract.Requires(client != null);
            try
            {
                client.Pipeline.ReadAction  = this.Pipeline.ReadAction;
                client.ReadStart();
                this.connectionHandler?.Invoke(client, error);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.GetType().Name} connection handler invocation failed.", exception);
            }
        }

        protected abstract StreamHandle NewStream();

        void OnReadCallback(ref uv_buf_t buf, int status)
        {
            /*
             *  nread is > 0 if there is data available or < 0 on error.
             *  When we’ve reached EOF, nread will be set to UV_EOF.
             *  When nread < 0, the buf parameter might not point to a valid buffer; 
             *  in that case buf.len and buf.base are both set to 0
             */
            // For status = 0 (Nothing to read)
            if (status >= 0) 
            {
                Log.Debug($"{this.HandleType} {this.InternalHandle} read, length = {buf.Length} status = {status}.");
                this.Pipeline.OnReadCompleted(buf.Memory, status);
                return;
            }

            Exception exception = null;
            if (status != (int)uv_err_code.UV_EOF) // Stream end is not an error
            {
                exception = NativeMethods.CreateError((uv_err_code)status);
                Log.Error($"{this.HandleType} {this.InternalHandle} read error, status = {status}", exception);
            }

            Log.Debug($"{this.HandleType} {this.InternalHandle} read completed.");
            this.Pipeline.OnReadCompleted(exception);
            this.ReadStop();
        }

        static void OnReadCallback(IntPtr handle, IntPtr nread, ref uv_buf_t buf)
        {
            var stream = HandleContext.GetTarget<StreamHandle>(handle);
            stream.OnReadCallback(ref buf, (int)nread.ToInt64());
        }

        void OnAllocateCallback(out uv_buf_t buf)
        {
            BufferBlock block = this.Pipeline.GetReadBuffer();
            buf = new uv_buf_t(block.AsPointer(), block.Count);
            Log.Debug($"{this.HandleType} {this.InternalHandle} buffer allocated, size = {buf.Length}.");
        }

        static void OnAllocateCallback(IntPtr handle, IntPtr suggestedSize, out uv_buf_t buf)
        {
            var stream = HandleContext.GetTarget<StreamHandle>(handle);
            stream.OnAllocateCallback(out buf);
        }
    }
}
