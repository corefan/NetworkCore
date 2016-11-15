namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetworkCore.Libuv.Common;
    using NetworkCore.Libuv.Logging;
    using NetworkCore.Libuv.Requests;

    sealed class WriteTaskQueue : IDisposable
    {
        static readonly ILog Log = LogFactory.ForContext<WriteTaskQueue>();

        const int MaximumPoolSize = 128;
        readonly MpscArrayQueue<WriteTask> requestPool;

        internal WriteTaskQueue()
        {
            this.requestPool = new MpscArrayQueue<WriteTask>(MaximumPoolSize);
        }

        internal void Enqueue(StreamHandle handle, byte[] array, int offset, int count, Action<Exception> completion)
        {
            Contract.Requires(handle != null);
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            WriteTask request;
            if (!this.requestPool.TryDequeue(out request))
            {
                request = new WriteTask(this.OnCompleted);
            }

            request.CompletionAction = completion;

            try
            {
                request.Prepare(array, offset, count);
                handle.WriteStream(request);
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(WriteTaskQueue)} {request} write error.", exception);
                request.Release();
                request.CompletionAction = null;
                throw;
            }
        }

        void OnCompleted(WriteTask writeTask, Exception error)
        {
            if (writeTask == null)
            {
                return;
            }
            
            writeTask.CompletionAction?.Invoke(error);
            writeTask.CompletionAction = null;
            if (this.requestPool.TryEnqueue(writeTask))
            {
                return;
            }

            writeTask.Dispose();
            Log.Trace($"{nameof(WriteTaskQueue)} Local write task pool is full Maximum = ({MaximumPoolSize}).");
        }

        public void Dispose() => this.requestPool.Clear();
    }
}
