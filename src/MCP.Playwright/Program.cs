using McpDotNet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose() // Capture all log levels
    .WriteTo.File(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log_.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

try
{
    Log.Information("Starting server...");

    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithTools();

    var app = builder.Build();
    await app.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");

    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}



