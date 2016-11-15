namespace NetworkCore.Libuv.Tests.Performance
{
    using System;
    using System.Text;
    using NetworkCore.Libuv.Buffers;
    using NetworkCore.Libuv.Handles;

    sealed class TcpPingPong : IDisposable
    {
        const string PingMessage = "PING";
        const char SplitToken = '\n';
        const int DurationInMilliseconds = 5000;

        readonly byte[] content;

        EchoServer server;

        Loop loop;
        long startTime;

        int pongs;
        int state;

        public TcpPingPong()
        {
            this.content = Encoding.UTF8.GetBytes(PingMessage + SplitToken);
        }

        public void Run()
        {
            this.pongs = 0;
            this.state = 0;
            this.server = new EchoServer();

            this.loop = this.server.Loop;
            this.StartClient();
        }

        void StartClient()
        {
            Tcp tcp = this.loop
                .CreateTcp()
                .Bind(EchoServer.LoopbackOnAnyPort, this.OnRead)
                .ConnectTo(EchoServer.EndPoint, this.WritePing);

            this.startTime = this.loop.Now;
            this.loop.RunDefault();

            long count = (long)Math.Floor((1000d * this.pongs) / DurationInMilliseconds);
            Console.WriteLine($"Ping pong : {count} roundtrips/s");

            tcp.Dispose();
        }

        void WritePing(Tcp tcp, Exception error)
        {
            if (error == null)
            {
                tcp.QueueWrite(this.content, WritePingCompleted);
                return;
            }

            Console.WriteLine($"Ping pong : failed, read error {error}.");
            tcp.Dispose();
        }

        static void WritePingCompleted(Tcp tcp, Exception error)
        {
            if (error == null)
            {
                return;
            }

            tcp.Dispose();
            Console.WriteLine($"Ping pong : Write ping failed, read error {error}.");
        }

        void OnRead(Tcp tcpClient, IReadCompletion completion)
        {
            if (completion.Error != null)
            {
                Console.WriteLine($"Ping pong : failed, read error {completion.Error}.");
                tcpClient.Dispose();
                return;
            }

            IRange data = completion.Data;
            if (data == null)
            {
                return;
            }

            string message = data.GetString(Encoding.UTF8);
            foreach (char token in message)
            {
                if (token == SplitToken)
                {
                    this.state = 0;
                }
                else
                {
                    if (token != PingMessage[this.state])
                    {
                        Console.WriteLine($"Ping pong : failed, wrong message token received {token}.");
                        tcpClient.Dispose();
                        return;
                    }

                    this.state++;
                }

                if (this.state == 0)
                {
                    this.pongs++;
                    long duration = this.loop.Now - this.startTime;
                    if (duration > DurationInMilliseconds)
                    {
                        tcpClient.Dispose();
                        this.server.Dispose();
                        return;
                    }
                    else
                    {
                        this.WritePing(tcpClient, null);
                    }
                }
            }
        }

        public void Dispose()
        {
            this.server = null;
            this.loop = null;
        }
    }
}
