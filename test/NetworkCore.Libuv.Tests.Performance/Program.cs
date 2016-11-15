namespace NetworkCore.Libuv.Tests.Performance
{
    using System;

    public class Program
    {
        const string LoopCategory = "loop";
        const string TcpCategory = "tcp";

        public static void Main(string[] args)
        {
            string category = args.Length > 0 ? args[0] : null;
            string name = args.Length > 1 ? args[1] : null;

            Run(category, name);
        }

        public static void Run(string category, string name)
        {
            if (string.IsNullOrEmpty(category)
                || string.Compare(category, LoopCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunLoopBenchmark(name);
            }

            if (string.IsNullOrEmpty(category)
                || string.Compare(category, TcpCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunTcpBenmark(name);
            }
        }

        public static void RunLoopBenchmark(string name)
        {
            Console.WriteLine($"{LoopCategory}");

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "count", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine("count");
                using (var loopCount = new LoopCount())
                {
                    loopCount.Run();
                }
            }
        }

        public static void RunTcpBenmark(string name)
        {
            Console.WriteLine($"{TcpCategory}");
            if (string.IsNullOrEmpty(name) 
                || string.Compare(name, "writeBatch", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine("writeBatch");
                using (var tcpWriteBatch = new TcpWriteBatch())
                {
                    tcpWriteBatch.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pingPong", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine("pingPong");
                using (var tcpPingPong = new TcpPingPong())
                {
                    tcpPingPong.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pump", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine("pump 1 client");
                using (var tcpPump = new TcpPump(1))
                {
                    tcpPump.Run();
                }

                Console.WriteLine("pump 100 client");
                using (var tcpPump = new TcpPump(100))
                {
                    tcpPump.Run();
                }
            }
        }
    }
}
