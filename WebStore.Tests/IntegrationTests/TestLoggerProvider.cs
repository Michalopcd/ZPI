using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WebStore.Tests.IntegrationTests
{
    public record TestLogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<TestLogEntry> _entries = new();

        public IReadOnlyCollection<TestLogEntry> Entries => _entries.ToArray();

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _entries);
        }

        public void Dispose()
        {
        }

        private class TestLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly ConcurrentQueue<TestLogEntry> _entries;

            public TestLogger(string categoryName, ConcurrentQueue<TestLogEntry> entries)
            {
                _categoryName = categoryName;
                _entries = entries;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _entries.Enqueue(new TestLogEntry(_categoryName, logLevel, formatter(state, exception), exception));
            }
        }

        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
