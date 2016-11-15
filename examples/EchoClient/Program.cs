namespace EchoClient
{
    using System;
    using System.Net;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using NetworkCore.Libuv.Buffers;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Logging;

    public class Program
    {
        const int Port = 9988;
        static Loop loop;

        public static void Main(string[] args)
        {
            LogFactory.AddConsoleProvider(LogLevel.Trace);

            try
            {
                RunLoop();

                loop.Dispose();
                loop = null;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Echo client error {exception}");
            }

            Console.WriteLine("Press any key to terminate the client");
            Console.ReadLine();
        }

        static void RunLoop()
        {
            loop = new Loop();

            var localEndPoint = new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort);
            var remoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, Port);
            Tcp tcp = loop
                .CreateTcp()
                .NoDelay(true)
                .Bind(localEndPoint, OnRead)
                .ConnectTo(remoteEndPoint, SendMessage);

            Console.WriteLine("Echo client loop starting.");
            loop.RunDefault();
            Console.WriteLine("Echo client loop dropped out");

            tcp.Dispose();
        }

        static void OnRead(Tcp tcpClient, IReadCompletion completion)
        {
            if (completion.Error != null)
            {
                Console.WriteLine($"Echo client error {completion.Error}");
                tcpClient.Dispose();
                return;
            }

            IRange data = completion.Data;
            if (data.Count == 0)
            {
                return;
            }

            string message = data.GetString(Encoding.UTF8);
            Console.WriteLine($"Echo client received : {message}");

            Console.WriteLine("Message received, sending Q to server");
            tcpClient.QueueWrite(Encoding.UTF8.GetBytes("Q"), OnWriteCompleted);
        }

        static void SendMessage(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Echo client error {error}");
                tcp.Dispose();
                return;
            }

            Console.WriteLine("Echo client connected, request write message.");
            byte[] bytes = Encoding.UTF8.GetBytes($"Greetings {DateTime.UtcNow}");
            tcp.QueueWrite(bytes, OnWriteCompleted);
        }

        static void OnWriteCompleted(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Echo client error {error}");
                tcp.Dispose();
            }
            else
            {
                Console.WriteLine("Echo client sending message completed sucessfully.");
            }
        }
    }
}
