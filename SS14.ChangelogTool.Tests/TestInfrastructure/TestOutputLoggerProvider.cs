using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SS14.ChangelogTool.Tests.TestInfrastructure;

public sealed class TestOutputLoggerProvider(ITestOutputHelper outputHelper) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new TestOutputLogger(outputHelper, categoryName);

    public void Dispose() { }
}