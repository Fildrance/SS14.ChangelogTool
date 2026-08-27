using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SS14.ChangelogTool.Tests.TestInfrastructure;

public sealed class TestOutputLogger(ITestOutputHelper outputHelper, string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        outputHelper.WriteLine($"[{logLevel}] {categoryName}: {formatter(state, exception)}");
        if (exception is not null)
            outputHelper.WriteLine(exception.ToString());
    }
}