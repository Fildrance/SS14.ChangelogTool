using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SS14.ChangelogTool;

var services = new ServiceCollection();
services.RegisterDependencies();
using var serviceProvider = services.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Console logging configured");

var rootCommand = serviceProvider.GetRequiredService<RootCommand>();
return await WrapWithExceptionHandling(rootCommand.Parse(args), logger); 

static async Task<int> WrapWithExceptionHandling(ParseResult parseResult, ILogger logger)
{
    var startTime = DateTime.UtcNow;
    logger.LogDebug("[Log] Starting command execution...");

    try
    {
        var config = new InvocationConfiguration
        {
            EnableDefaultExceptionHandler = false
        };

        return await parseResult.InvokeAsync(config);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Global Error] Intercepted Exception");

        return 1;
    }
    finally
    {
        var duration = DateTime.UtcNow - startTime;
        logger.LogDebug("[Log] Execution completed in {duration}", duration);
    }
}