using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Practice.Lab4;

/// <summary>Ghi log vừa console vừa file (bằng chứng lab), không dùng Serilog để giữ lab độc lập.</summary>
public static class LabLog
{
    public static ILoggerFactory Factory { get; } = LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddProvider(new FileProvider(LabConfig.LogFile));
    });

    public static ILogger<T> For<T>() => Factory.CreateLogger<T>();

    public static ILogger For(string category) => Factory.CreateLogger(category);

    private sealed class FileProvider(string path) : ILoggerProvider
    {
        private static readonly Lock FileLock = new();

        public ILogger CreateLogger(string categoryName) => new FileLogger(path, categoryName);

        public void Dispose() { }

        private sealed class FileLogger(string path, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                var line = $"[{DateTime.Now:HH:mm:ss} {LogLevelShort(logLevel)}] [{category}] {formatter(state, exception)}";
                if (exception is not null) line += Environment.NewLine + exception;
                lock (FileLock)
                {
                    Console.WriteLine(line);
                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }

            private static string LogLevelShort(LogLevel level) => level switch
            {
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Debug => "DBG",
                _ => level.ToString().ToUpperInvariant()
            };
        }
    }
}
