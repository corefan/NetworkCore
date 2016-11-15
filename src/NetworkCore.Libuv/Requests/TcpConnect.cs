namespace NetworkCore.Libuv.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Native;

    sealed class TcpConnect : IDisposable
    {
        readonly WatcherRequest watcherRequest;

        Action<Tcp, Exception> connectedAction;

        public TcpConnect(Tcp tcp, IPEndPoint remoteEndPoint, Action<Tcp, Exception> connectedAction)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(connectedAction != null);

            tcp.Validate();

            this.Tcp = tcp;
            this.connectedAction = connectedAction;

            this.watcherRequest = new WatcherRequest(
                uv_req_type.UV_CONNECT,
                this.OnConnected,
                h => NativeMethods.TcpConnect(h, tcp.InternalHandle, remoteEndPoint));
        }

        internal Tcp Tcp { get; private set; }

        void OnConnected(WatcherRequest request, Exception error)
        {
            if (this.Tcp == null 
                || this.connectedAction == null)
            {
                throw new ObjectDisposedException($"{nameof(TcpConnect)} has already been disposed.");
            }

            try
            {
                this.connectedAction(this.Tcp, error);
                if (error == null)
                {
                    this.Tcp.ReadStart();
                }
            }
            catch (Exception exception)
            {
                ScheduleRequest.Log.Error("UV_CONNECT callback error.", exception);
            }
            finally
            {
                this.Dispose();
            }
        }

        public void Dispose()
        {
            this.Tcp = null;
            this.connectedAction = null;
            this.watcherRequest.Dispose();
        }
    }
}
