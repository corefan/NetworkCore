using System;

namespace NetworkCore.Libuv.Tests.Performance
{
    using System.Collections.Generic;
    using System.Net;
    using NetworkCore.Libuv.Handles;

    sealed class BlackholeServer : IDisposable
    {
        const int MaximumBacklogSize = 1000;

        public static readonly int Port = 9089;
        public static readonly int DefaultTimeoutInMilliseconds = 500;
        public static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, Port);
        public static readonly IPEndPoint LoopbackOnAnyPort = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MinPort);
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(DefaultTimeoutInMilliseconds);

        readonly List<Tcp> connections;
        readonly Tcp tcpServer;

        public BlackholeServer()
        {
            this.connections = new List<Tcp>();

            this.Loop = new Loop();
            this.tcpServer = this.Loop
                .CreateTcp()
                .Bind(EndPoint, OnRead)
                .Listen(this.OnConnection, MaximumBacklogSize);
        }

        static void OnRead(Tcp tcpClient, IReadCompletion completion)
        {
            // NOP
        }

        internal Loop Loop { get; }

        void OnConnection(Tcp tcp, Exception error)
        {
            if (error == null)
            {
                this.connections.Add(tcp);
                return;
            }

            Console.WriteLine($"Blackhole server client connection failed {error}");
            tcp?.Dispose();
        }

        public void Dispose()
        {
            this.tcpServer.Dispose();
            this.connections.Clear();
            this.Loop.Dispose();
        }
    }
}
