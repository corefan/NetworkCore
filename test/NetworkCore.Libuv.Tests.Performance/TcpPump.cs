namespace NetworkCore.Libuv.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Native;

    sealed class TcpPump : IDisposable
    {
        const int Port = 9889;
        const int WriteBufferSize = 8192;
        const int StatisticsCount = 5;
        const int StatisticsInterval = 1000; // milliseconds
        const int MaximumWriteHandles = 1000; // backlog size

        static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, Port);
        static readonly IPEndPoint LoopbackOnAnyPort = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MinPort);

        readonly List<Tcp> connections;
        readonly List<Tcp> connected;
        readonly int clientCount;

        int connectedClientCount;

        byte[] data;
        Loop loop;
        Tcp tcpServer;
        Timer timer;

        long bytesRead;
        long totalBytesRead;

        long bytesWrite;
        long totalBytesWrite;


        int statisticsCount;

        long startTime;
        long stopTime;
        readonly bool showIntervalStatistics;

        public TcpPump(int clientCount, bool showIntervalStatistics = false)
        {
            this.clientCount = clientCount;
            this.connections = new List<Tcp>();
            this.connected = new List<Tcp>();
            this.data = new byte[WriteBufferSize];
            this.showIntervalStatistics = showIntervalStatistics;
        }

        static double ToGigaBytes(long total, long interval)
        {
            double bits = total * 8;

            bits /= 1024;
            bits /= 1024;
            bits /= 1024;

            double duration = interval / 1000d;
            return bits / duration;
        }

        public void Run()
        {
            this.StartServer();
            this.StartClient();

            try
            {
                this.loop.RunDefault();
            }
            catch (OperationException exception)
            {
                // Pipe broken and Operation cancelled
                // are all ok here.
                if (exception.ErrorCode != -4047
                    || exception.ErrorCode != -4081)
                {
                    throw;
                }
            }

            long diff = (this.stopTime - this.startTime);
            double total = ToGigaBytes(this.totalBytesWrite, diff);

            Console.WriteLine($"tcp pump {this.clientCount} client : {total} gbit/s");

            total = ToGigaBytes(this.totalBytesRead, diff);
            Console.WriteLine($"tcp pump {this.clientCount} server : {total} gbit/s");
        }

        void StartClient()
        {
            for (int i = 0; i < this.clientCount; i++)
            {
                Tcp tcp = this.loop
                    .CreateTcp()
                    .Bind(LoopbackOnAnyPort, OnClientRead)
                    .ConnectTo(EndPoint, this.OnConnected);

                this.connected.Add(tcp);
            }
        }

        static void OnClientRead(Tcp tcpClient, IReadCompletion completion)
        {
            // NOP
        }

        void OnConnected(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"tcp pump {this.clientCount} failed, {error}");
                tcp.Dispose();
                return;
            }

            this.connectedClientCount++;

            // Wait until all connected
            if (this.connectedClientCount == this.clientCount)
            {
                this.StartStatistics();
                foreach (Tcp tcpClient in this.connected)
                {
                    this.Write(tcpClient);
                }
            }
        }

        void StartStatistics()
        {
            this.statisticsCount = StatisticsCount;
            this.timer = this.loop.CreateTimer();
            this.timer.Start(this.ShowStatistics, StatisticsInterval, StatisticsInterval);

            this.loop.UpdateTime();
            this.startTime = this.loop.Now;
        }

        void ShowStatistics(Timer handle)
        {
            if (this.showIntervalStatistics)
            {
                Console.WriteLine($"tcp pump connections : {this.connectedClientCount}, write : {ToGigaBytes(this.bytesWrite, StatisticsInterval)} gbit/s, read : {ToGigaBytes(this.bytesRead, StatisticsInterval)} gbit/s.");
            }

            this.statisticsCount--;

            if (this.statisticsCount > 0)
            {
                this.bytesWrite = 0;
                this.bytesRead = 0;
                return;
            }

            this.loop.UpdateTime();
            this.stopTime = this.loop.Now;

            handle.Stop();
            handle.Dispose();

            this.connected.ForEach(x => x.Shutdown(OnShutdown));
            this.connections.ForEach(x => x.Shutdown(OnShutdown));

            this.tcpServer.Shutdown(OnShutdown);
        }

        static void OnShutdown(Tcp tcp, Exception error) => tcp.Dispose();

        void Write(Tcp tcp)
        {
            if (this.statisticsCount > 0)
            {
                tcp.QueueWrite(this.data, this.OnWriteComplete);
            }
        }

        void OnWriteComplete(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"tcp pump {this.clientCount} failed, {error}");
                tcp.Dispose();
                return;
            }

            this.totalBytesWrite += WriteBufferSize;
            this.bytesWrite += WriteBufferSize;
            this.Write(tcp);
        }

        void StartServer()
        {
            this.loop = new Loop();

            this.tcpServer = this.loop
                .CreateTcp()
                .SimultaneousAccepts(true)
                .Bind(EndPoint, this.OnServerRead)
                .Listen(this.OnConnection, MaximumWriteHandles);
        }

        void OnServerRead(Tcp tcpClient, IReadCompletion completion)
        {
            if (this.totalBytesRead == 0)
            {
                this.loop.UpdateTime();
                this.startTime = this.loop.Now;
            }
            int count = completion.Data.Count;
            this.bytesRead += count;
            this.totalBytesRead += count;
        }

        void OnConnection(Tcp tcp, Exception error)
        {
            if (error == null)
            {
                this.connections.Add(tcp);
                return;
            }

            Console.WriteLine($"tcp pump {this.clientCount} failed, {error}");
            tcp.Dispose();
        }

        public void Dispose()
        {
            this.connected.Clear();
            this.connections.Clear();

            this.data = null;
            this.tcpServer.Dispose();
            this.loop.Dispose();

            this.tcpServer = null;
            this.loop = null;
        }
    }
}
