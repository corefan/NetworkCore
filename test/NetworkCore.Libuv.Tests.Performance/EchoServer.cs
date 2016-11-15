namespace NetworkCore.Libuv.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using NetworkCore.Libuv.Buffers;
    using NetworkCore.Libuv.Handles;

    sealed class EchoServer : IDisposable
    {
        const int MaximumBacklogSize = 1000;

        public static readonly int Port = 9889;
        public static readonly int DefaultTimeoutInMilliseconds = 500;
        public static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, Port);
        public static readonly IPEndPoint LoopbackOnAnyPort = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MinPort);
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(DefaultTimeoutInMilliseconds);

        readonly List<Tcp> connections;
        readonly Tcp tcpServer;

        public EchoServer()
        {
            this.connections = new List<Tcp>();

            this.Loop = new Loop();
            this.tcpServer = this.Loop
                .CreateTcp()
                .Bind(EndPoint, this.OnRead)
                .Listen(this.OnConnection, MaximumBacklogSize);
        }

        void OnRead(Tcp tcpClient, IReadCompletion completion)
        {
            if (completion.Error != null)
            {
                Console.WriteLine($"{nameof(EchoServer)} read failed, {completion.Error}");
                tcpClient.Dispose();
                return;
            }

            IRange data = completion.Data;
            if (data.Count == 0)
            {
                return;
            }

            string message = data.GetString(Encoding.UTF8);
            /*
             * Scan for the letter Q which signals that we should quit the server.
             * If we get QS it means close the stream.
             */
            if (message.StartsWith("Q"))
            {
                tcpClient.Dispose();

                if (message.EndsWith("QS"))
                {
                    this.Dispose();
                }
            }
            else
            {
                byte[] array = data.Copy();
                tcpClient.QueueWrite(array, this.OnWriteCompleted);
            }
        }

        void OnWriteCompleted(Tcp tcpClient, Exception error)
        {
            if (error == null)
            {
                return;
            }

            Console.WriteLine($"{nameof(EchoServer)} client connection failed, {error}");
            tcpClient?.Dispose();
            if (this.connections.Contains(tcpClient))
            {
                this.connections.Remove(tcpClient);
            }
        }

        internal Loop Loop { get; }

        void OnConnection(Tcp tcp, Exception error)
        {
            if (error == null)
            {
                this.connections.Add(tcp);
                return;
            }

            Console.WriteLine($"{nameof(EchoServer)} client connection failed, {error}");
            tcp?.Dispose();
        }

        public void Dispose()
        {
            this.connections.ForEach(x => x.Dispose());
            this.tcpServer.Dispose();
            this.Loop.Dispose();
            this.connections.Clear();
        }
    }
}
