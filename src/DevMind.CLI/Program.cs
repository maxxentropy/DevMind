using DevMind.CLI.Configuration;
using DevMind.CLI.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevMind.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();

            // Handle special commands
            if (args.Length > 0)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "test":
                        var testCommand = host.Services.GetRequiredService<TestCommand>();
                        return await testCommand.ExecuteAsync();
                    
                    case "version":
                    case "--version":
                    case "-v":
                        var versionCommand = host.Services.GetRequiredService<VersionCommand>();
                        return await versionCommand.ExecuteAsync();
                    
                    case "help":
                    case "--help":
                    case "-h":
                        ShowHelp();
                        return 0;
                }
            }

            // Default to process command
            var processCommand = host.Services.GetRequiredService<ProcessCommand>();
            return await processCommand.ExecuteAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("DevMind AI Development Agent");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  devmind [command] [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  test                 Test DevMind foundation");
        Console.WriteLine("  version              Show version information");
        Console.WriteLine("  help                 Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  devmind test");
        Console.WriteLine("  devmind \"analyze my repository\"");
        Console.WriteLine("  devmind version");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables("DEVMIND_")
                      .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddCliServices(context.Configuration);
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders()
                       .AddConfiguration(context.Configuration.GetSection("Logging"))
                       .AddConsole()
                       .AddDebug();
            });
}
