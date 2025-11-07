using FileContentRenamer.Models;
using FileContentRenamer.Services;
using FileContentRenamer.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace FileContentRenamer
{
    static class Program
    {
        private const int SuccessExitCode = 0;
        private const int ErrorExitCode = 1;

        static async Task<int> Main(string[] args)
        {
            // Get the absolute path to the solution directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            string solutionDir = GetSolutionDirectory(baseDir);

            // Create the logs directory in the solution folder
            string logsPath = Path.Combine(solutionDir, "logs");
            Directory.CreateDirectory(logsPath);

            // Configure logging
            SetupLogger(solutionDir, logsPath);

            Log.Information("File Content Renamer Tool starting up");

            try
            {
                // Get command-line argument for base path if provided
                string? cmdLineBasePath = args.Length > 0 ? args[0] : null;

                // Load app configuration from file
                var config = AppConfig.LoadFromConfiguration(cmdLineBasePath);

                Log.Debug("Using Tesseract data path: {TesseractDataPath}", config.TesseractDataPath);
                Log.Debug("Using Tesseract language: {TesseractLanguage}", config.TesseractLanguage);

                // Resolve and configure the directory path
                ResolveDirectoryPath(args, config);

                // Validate directory path
                if (string.IsNullOrEmpty(config.BasePath))
                {
                    throw new InvalidOperationException("Base path is not configured");
                }

                ValidateDirectoryPath(config.BasePath);

                Log.Debug("Directory validated: {BasePath}", config.BasePath);

                // Log whether subdirectories will be included (from configuration)
                Log.Debug("Include subdirectories (from config): {IncludeSubdirectories}", config.IncludeSubdirectories);

                // Configure services using DI container
                using var serviceProvider = ServiceConfiguration.ConfigureServices(config);

                // Resolve the main service from the container
                var fileService = serviceProvider.GetRequiredService<IFileService>();

                Log.Information("Starting file processing");

                await fileService.ProcessFilesAsync();

                Log.Information("Processing completed successfully");
                return SuccessExitCode;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during processing");
                return ErrorExitCode;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        /// <summary>
        /// Resolve the directory path from command line arguments or configuration
        /// </summary>
        private static void ResolveDirectoryPath(string[] args, AppConfig config)
        {
            if (args.Length > 0)
            {
                config.BasePath = args[0];
                config.SaveLastUsedPath(config.BasePath);
            }
            else
            {
                ResolveDirectoryPathFromConfig(config);
            }
        }

        /// <summary>
        /// Resolve directory path from configuration or user input
        /// </summary>
        private static void ResolveDirectoryPathFromConfig(AppConfig config)
        {
            // If we have a valid LastUsedPath, use it directly without prompting
            if (!string.IsNullOrEmpty(config.LastUsedPath) && Directory.Exists(config.LastUsedPath))
            {
                config.BasePath = config.LastUsedPath;
                Log.Debug("Automatically using last used directory path: {BasePath}", config.BasePath);
            }
            else
            {
                PromptUserForPath(config);
            }
        }

        /// <summary>
        /// Prompt user for directory path input
        /// </summary>
        private static void PromptUserForPath(AppConfig config)
        {
            string? defaultPath = !string.IsNullOrEmpty(config.LastUsedPath) ? config.LastUsedPath : config.BasePath;
            Log.Debug("Default path selection: LastUsedPath='{LastUsedPath}', BasePath='{BasePath}', Selected='{DefaultPath}'",
                config.LastUsedPath, config.BasePath, defaultPath);

            string prompt = $"Enter directory path to scan (leave blank for default: '{config.BasePath}'): ";
            Console.Write(prompt);
            string? path = Console.ReadLine();

            ProcessUserInput(path, config);
        }

        /// <summary>
        /// Process user input and update configuration
        /// </summary>
        private static void ProcessUserInput(string? path, AppConfig config)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (Directory.Exists(path))
                {
                    config.BasePath = path;
                    Log.Debug("Using user-provided directory path: {BasePath}", config.BasePath);
                    config.SaveLastUsedPath(config.BasePath);
                }
                else
                {
                    HandleInvalidPath(path, config);
                }
            }
            else
            {
                Log.Debug("Using default directory path: {BasePath}", config.BasePath);
            }
        }

        /// <summary>
        /// Handle invalid directory path
        /// </summary>
        private static void HandleInvalidPath(string path, AppConfig config)
        {
            Log.Error("Directory not found: {Path}", path);

            // Fall back to last used path if available, or default
            if (!string.IsNullOrEmpty(config.LastUsedPath) && Directory.Exists(config.LastUsedPath))
            {
                config.BasePath = config.LastUsedPath;
                Log.Debug("Using last used path instead: {Path}", config.BasePath);
            }
            else
            {
                Log.Debug("Using default directory path: {BasePath}", config.BasePath);
            }
        }

        /// <summary>
        /// Validate that the directory path exists
        /// </summary>
        private static void ValidateDirectoryPath(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                Log.Error("Directory not found: {BasePath}", basePath);
                throw new DirectoryNotFoundException($"Directory not found: {basePath}");
            }
        }

        /// <summary>
        /// Find the solution directory by searching for a marker file
        /// </summary>
        private static string GetSolutionDirectory(string baseDir)
        {
            // Try to find the solution directory by traversing up from the current directory
            var currentDir = new DirectoryInfo(baseDir);
            while (currentDir != null)
            {
                // Check for markers that would indicate this is the solution root
                if (File.Exists(Path.Combine(currentDir.FullName, "comprobantes.sln")) ||
                    (Directory.Exists(Path.Combine(currentDir.FullName, "FileContentRenamer")) &&
                     Directory.Exists(Path.Combine(currentDir.FullName, "tessdata"))))
                {
                    return currentDir.FullName;
                }

                // Move up one directory level
                currentDir = currentDir.Parent;
            }

            // Fallback: Use the fixed number of directory traversals
            return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        }

        /// <summary>
        /// Configure the Serilog logger
        /// </summary>
        private static void SetupLogger(string solutionDir, string logsPath)
        {
            // Load configuration from appsettings.json
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(solutionDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfiguration configuration = configBuilder.Build();

            // Configure Serilog using appsettings.json, with sensible defaults if missing
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "FileContentRenamer")
                .Enrich.WithProperty("Version", "1.0.0")
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("UserName", Environment.UserName);

            // If file sink path isn't configured, ensure logs go to solution logs folder
            var logFilePath = Path.Combine(logsPath, "app.log");
            if (!File.Exists(Path.Combine(solutionDir, "appsettings.json")))
            {
                loggerConfig = loggerConfig
                    .MinimumLevel.Is(LogEventLevel.Information)
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, shared: true);
            }

            Log.Logger = loggerConfig.CreateLogger();
        }
    }
}
