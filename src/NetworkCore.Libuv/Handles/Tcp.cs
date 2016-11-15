namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetworkCore.Libuv.Native;
    using NetworkCore.Libuv.Requests;

    public sealed class Tcp : StreamHandle
    {
        internal const int DefaultBacklog = 128;

        internal Tcp(LoopContext loop)
            : base(loop, uv_handle_type.UV_TCP)
        { }

        public void Shutdown(Action<Tcp, Exception> completedAction = null) => 
            this.ShutdownStream((state, error) => completedAction?.Invoke((Tcp)state, error));

        public Tcp ConnectTo(IPEndPoint remoteEndPoint, Action<Tcp, Exception> connectedAction)
        {
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(connectedAction != null);

            TcpConnect request = null;
            try
            {
                request = new TcpConnect(this, remoteEndPoint, connectedAction);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} {this.InternalHandle} Failed to connect to {remoteEndPoint}", exception);
                request?.Dispose();
                throw;
            }

            return this;
        }

        public void QueueWrite(byte[] array, Action<Tcp, Exception> completedAction = null)
        {
            Contract.Requires(array != null);

            this.QueueWrite(array, 0, array.Length, completedAction);
        }

        public void QueueWrite(byte[] array, int offset, int count, Action<Tcp, Exception> completion = null)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            this.QueueWriteStream(array, offset, count, 
                (state, error) => completion?.Invoke((Tcp)state, error));
        }

        internal Tcp RegisterRead(Action<Tcp, IReadCompletion> readAction)
        {
            Contract.Requires(readAction != null);

            this.Pipeline.ReadAction = 
                (stream, completion) => readAction.Invoke((Tcp)stream, completion);
            return this;
        }

        internal Tcp Bind(IPEndPoint endPoint, bool dualStack = false)
        {
            Contract.Requires(endPoint != null);

            this.Validate();
            NativeMethods.TcpBind(this.InternalHandle, endPoint, dualStack);

            return this;
        }

        public IPEndPoint GetLocalEndPoint()
        {
            this.Validate();
            return NativeMethods.TcpGetSocketName(this.InternalHandle);
        }

        public IPEndPoint GetPeerEndPoint()
        {
            this.Validate();
            return NativeMethods.TcpGetPeerName(this.InternalHandle);
        }

        public Tcp NoDelay(bool value)
        {
            this.Validate();
            NativeMethods.TcpSetNoDelay(this.InternalHandle, value);

            return this;
        }

        public Tcp KeepAlive(bool value, int delay)
        {
            this.Validate();
            NativeMethods.TcpSetKeepAlive(this.InternalHandle, value, delay);

            return this;
        }

        public Tcp SimultaneousAccepts(bool value)
        {
            this.Validate();
            NativeMethods.TcpSimultaneousAccepts(this.InternalHandle, value);

            return this;
        }

        protected override unsafe StreamHandle NewStream()
        {
            IntPtr loopHandle = ((uv_stream_t*)this.InternalHandle)->loop;
            var loop = HandleContext.GetTarget<LoopContext>(loopHandle);
            return new Tcp(loop);
        }
    }
}
