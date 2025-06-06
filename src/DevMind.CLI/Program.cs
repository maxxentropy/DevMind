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

            // Create a scope for resolving scoped services
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            // Handle special commands
            if (args.Length > 0)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "test":
                        var testCommand = services.GetRequiredService<TestCommand>();
                        return await testCommand.ExecuteAsync();

                    case "llm-test":
                        var llmTestCommand = services.GetRequiredService<LlmTestCommand>();
                        return await llmTestCommand.ExecuteAsync();

                    case "config":
                        var configCommand = services.GetRequiredService<ConfigCommand>();
                        var configArgs = args.Skip(1).ToArray();
                        return await configCommand.ExecuteAsync(configArgs);

                    case "status":
                        var statusCommand = services.GetRequiredService<StatusCommand>();
                        return await statusCommand.ExecuteAsync();

                    case "version":
                    case "--version":
                    case "-v":
                        var versionCommand = services.GetRequiredService<VersionCommand>();
                        return await versionCommand.ExecuteAsync();

                    case "help":
                    case "--help":
                    case "-h":
                        ShowHelp();
                        return 0;
                }
            }

            // Default to process command
            var processCommand = services.GetRequiredService<ProcessCommand>();
            return await processCommand.ExecuteAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");

            // Log full exception details in debug mode
            if (args.Contains("--debug") || args.Contains("-d"))
            {
                Console.Error.WriteLine($"Exception details: {ex}");
            }

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
        Console.WriteLine("  llm-test             Test LLM service connectivity");
        Console.WriteLine("  config [subcommand]  Manage configuration");
        Console.WriteLine("  status               Check service status");
        Console.WriteLine("  version              Show version information");
        Console.WriteLine("  help                 Show this help message");
        Console.WriteLine();
        Console.WriteLine("Configuration subcommands:");
        Console.WriteLine("  validate             Validate current configuration");
        Console.WriteLine("  show [section]       Show configuration");
        Console.WriteLine("  providers            Show available LLM providers");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  devmind test");
        Console.WriteLine("  devmind llm-test");
        Console.WriteLine("  devmind config validate");
        Console.WriteLine("  devmind status");
        Console.WriteLine("  devmind \"analyze my repository\"");
        Console.WriteLine("  devmind version");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --debug, -d          Show detailed error information");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                if (context.HostingEnvironment.IsDevelopment() ||
                    context.HostingEnvironment.EnvironmentName.Equals("LocalDevelopment", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[DIAGNOSTIC] Loading user secrets for '{context.HostingEnvironment.EnvironmentName}' environment.");
                    config.AddUserSecrets<Program>();
                }

                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables("DEVMIND_")
                      .AddCommandLine(args.Where(arg => arg.StartsWith("--config") || arg.StartsWith("-c")).ToArray());
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

                // Reduce logging noise in production
                if (context.HostingEnvironment.IsProduction())
                {
                    logging.AddFilter("Microsoft", LogLevel.Warning);
                    logging.AddFilter("System", LogLevel.Warning);
                }
            });
}
