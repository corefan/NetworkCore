namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Buffers;
    using NetworkCore.Libuv.Logging;

    sealed class Pipeline : IDisposable
    {
        static readonly ILog Log = LogFactory.ForContext<Pipeline>();

        readonly StreamHandle streamHandle;
        readonly BufferBlock readBuffer;
        readonly WriteTaskQueue writeTaskQueue;

        Action<StreamHandle, IReadCompletion> readAction;

        internal Pipeline(StreamHandle streamHandle)
        {
            Contract.Requires(streamHandle != null);

            this.streamHandle = streamHandle;
            this.readBuffer = new BufferBlock();
            this.writeTaskQueue = new WriteTaskQueue();
        }

        internal Action<StreamHandle, IReadCompletion> ReadAction
        {
            get { return this.readAction; }
            set
            {
                if (this.readAction != null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(Pipeline)} channel data handler has already been registered");
                }

                this.readAction = value;
            }
        }

        internal BufferBlock GetReadBuffer()
        {
            this.readBuffer.Prepare();
            return this.readBuffer;
        }

        internal void OnReadCompleted(Exception exception = null)
        {
            if (exception != null)
            {
                this.InvokeRead(null, exception);
            }
        }

        internal void OnReadCompleted(IntPtr handle, int size)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(size >= 0);

            if (size == 0)
            {
                return;
            }

            IntPtr bufferHandle = this.readBuffer.AsPointer();
            if (bufferHandle != handle)
            {
                Log.Critical($"{nameof(Pipeline)} memory handle {bufferHandle} allocated is different from received buffer handle {handle}.");
                throw new InvalidOperationException($"{nameof(Pipeline)} Corrupted stream read memory buffer.");
            }

            BlockRange range = this.readBuffer.Range(size);
            this.InvokeRead(range);
        }

        void InvokeRead(BlockRange range, Exception error = null)
        {
            var completion = new ReadCompletion(range, error);
            try
            {
                this.ReadAction?.Invoke(this.streamHandle, completion);
            }
            catch (Exception exception)
            {
                Log.Warn($"{nameof(Pipeline)} Exception whilst invoking read callback.", exception);
            }
            finally
            {
                completion.Dispose();
            }
        }

        internal void QueueWrite(byte[] array, int offset, int count, Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            try
            {
                this.writeTaskQueue.Enqueue(
                    this.streamHandle,
                    array, offset, count,
                    error => this.OnWriteCompleted(completion, error));
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(Pipeline)} {this.streamHandle.HandleType} faulted.", exception);
                throw;
            }
        }

        void OnWriteCompleted(Action<StreamHandle, Exception> completion, Exception error)
        {
            try
            {
                completion?.Invoke(this.streamHandle, error);
            }
            catch (Exception exception)
            {
                Log.Warn($"{nameof(Pipeline)} Exception whilst invoking write callback.", exception);
            }
        }

        public void Dispose()
        {
            this.writeTaskQueue.Dispose();
            this.readBuffer.Dispose();
            this.readAction = null;
        }

        sealed class ReadCompletion : IReadCompletion, IDisposable
        {
            BlockRange data;

            internal ReadCompletion(BlockRange data, Exception error)
            {
                this.data = data;
                this.Error = error;
            }

            public IRange Data => this.data;

            public Exception Error { get; private set; }

            public void Dispose()
            {
                this.data.Dispose();
                this.data = null;
                this.Error = null;
            }
        }
    }
}
