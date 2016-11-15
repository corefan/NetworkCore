namespace NetworkCore.Libuv.Logging
{
    using System;
    using Microsoft.Extensions.Logging;

    public static class LogFactory
    {
        static readonly ILoggerFactory DefaultFactory;

        static LogFactory()
        {
            DefaultFactory = new LoggerFactory();
        }

        public static void AddConsoleProvider(LogLevel mininumLevel) => 
            DefaultFactory.AddConsole(mininumLevel);

        public static void AddProvider(ILoggerProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            DefaultFactory.AddProvider(provider);
        }

        public static ILog ForContext<T>() => ForContext(typeof(T).Name);

        public static ILog ForContext(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = $"Unkonwn {Guid.NewGuid()}";
            }

            ILogger logger = DefaultFactory.CreateLogger(name);
            return new DefaultLog(logger);
        }
    }
}
