namespace EchoServer
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using NetworkCore.Libuv.Buffers;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Logging;

    public class Program
    {
        const int Port = 9988;
        static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.IPv6Loopback, Port);

        static Loop loop;
        static Tcp tcp;
        static List<Tcp> connections;

        public static void Main(string[] args)
        {
            LogFactory.AddConsoleProvider(LogLevel.Trace);
            connections = new List<Tcp>();
            try
            {
                StartServer();
                loop.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Echo server error {exception}.");
            }

            Console.WriteLine("Press any key to terminate echo server.");
            Console.ReadLine();
        }

        static void StartServer()
        {
            loop = new Loop();
            tcp = loop
                .CreateTcp()
                .SimultaneousAccepts(true)
                .Bind(EndPoint, OnRead)
                .Listen(OnConnection);

            Console.WriteLine($"Echo server starting on {EndPoint}.");
            loop.RunDefault();
            Console.WriteLine("Echo server loop completed.");

            connections.ForEach(x => x.Dispose());
            connections.Clear();
        }

        static void OnConnection(Tcp tcpClient, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Echo server client connection failed {error}");
                tcpClient?.Dispose();
            }
            else
            {
                Console.WriteLine($"Echo server client connection accepted {tcpClient.GetPeerEndPoint()}");
                connections.Add(tcpClient);
            }
        }

        static void OnRead(Tcp tcpClient, IReadCompletion completion)
        {
            if (completion.Error != null)
            {
                Console.WriteLine($"Echo server read failed {completion.Error}");
                tcpClient.Dispose();
                return;
            }

            IRange data = completion.Data;
            if (data == null 
                || data.Count == 0)
            {
                return;
            }

            string message = data.GetString(Encoding.UTF8);
            Console.WriteLine($"Echo server received : {message}");

            /*
             * Scan for the letter Q which signals that we should quit the server.
             * If we get QS it means close the stream.
             */
            if (message.StartsWith("Q"))
            {
                Console.WriteLine("Echo server closing stream.");
                tcpClient.Dispose();

                if (message.EndsWith("QS"))
                {
                    return;
                }

                Console.WriteLine("Echo server shutting down.");
                tcp.Dispose();
            }
            else
            {
                Console.WriteLine("Echo server sending echo back.");
                byte[] array = Encoding.UTF8.GetBytes($"ECHO [{message}]");
                tcpClient.QueueWrite(array, OnWriteCompleted);
            }
        }

        static void OnWriteCompleted(Tcp tcpClient, Exception error)
        {
            Console.WriteLine(error != null ? 
                $"Echo server sending echo failed {error}." 
                : "Echo server sending echo completed.");

            tcpClient.Dispose();
        }
    }
}
