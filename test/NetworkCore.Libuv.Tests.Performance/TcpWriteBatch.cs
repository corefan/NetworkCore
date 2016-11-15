namespace NetworkCore.Libuv.Tests.Performance
{
    using System;
    using System.Text;
    using NetworkCore.Libuv.Handles;

    sealed class TcpWriteBatch : IDisposable
    {
        const string Content = "Hello, world.";
        const long NumberOfRequests = (1000 * 1000);
        const long NanoSeconds = 1000000000;

        readonly byte[] data;
        BlackholeServer server;
        Loop loop;

        long writeCount;
        long batchWriteCommit;
        long batchWriteFinish;

        public TcpWriteBatch()
        {
            this.data = Encoding.UTF8.GetBytes(Content);
            this.writeCount = 0;
        }

        public void Run()
        {
            this.server = new BlackholeServer();
            this.loop = this.server.Loop;
            this.StartClient();
        }

        void StartClient()
        {
            Tcp tcp = this.loop
                .CreateTcp()
                .Bind(BlackholeServer.LoopbackOnAnyPort, OnClientRead)
                .ConnectTo(BlackholeServer.EndPoint, this.SendWriteRequests);

            this.loop.RunDefault();

            long duration = this.batchWriteFinish - this.batchWriteCommit;
            Console.WriteLine($"Tcp write batch : {NumberOfRequests} write requests in {(double)duration / NanoSeconds} seconds.");

            tcp.Dispose();
        }

        static void OnClientRead(Tcp tcpClient, IReadCompletion completion)
        {
            // NOP
        }

        void OnWriteCompleted(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Tcp write batch : Write request failed {error}.");
                tcp.Dispose();
                return;
            }

            this.writeCount++;

            if (this.writeCount < NumberOfRequests)
            {
                return;
            }

            this.batchWriteFinish = this.loop.NowInHighResolution;
            tcp.Dispose();
            this.server.Dispose();
        }

        void SendWriteRequests(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Tcp write batch : Write request failed {error}.");
                tcp.Dispose();
                return;
            }

            for (int i = 0; i < NumberOfRequests; i++)
            {
                tcp.QueueWrite(this.data, this.OnWriteCompleted);
            }

            this.batchWriteCommit = this.loop.NowInHighResolution;
        }

        public void Dispose()
        {
            this.server = null;
            this.loop = null;
        }
    }
}
