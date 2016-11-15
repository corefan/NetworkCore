namespace NetworkCore.Libuv.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;

    public static class HandleExtensions
    {
        public static Tcp Listen(this Tcp tcp, 
            Action<Tcp, Exception> connectionHandler,
            int backlog = Tcp.DefaultBacklog)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(connectionHandler != null);

            tcp.Listen(backlog, 
                (stream, error) => connectionHandler((Tcp)stream, error));

            return tcp;
        }

        public static Tcp Bind(this Tcp tcp, 
            IPEndPoint localEndPoint,
            Action<Tcp, IReadCompletion> readAction, 
            bool dualStack = false)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(localEndPoint != null);
            Contract.Requires(readAction != null);

            tcp.Bind(localEndPoint, dualStack);
            tcp.RegisterRead(readAction);

            return tcp;
        }
    }
}
