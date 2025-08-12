using FileContentRenamer.Models;
using FileContentRenamer.Services;
using FileContentRenamer.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace FileContentRenamer
{
    class Program
    {
        static async Task Main(string[] args)
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
                
                Log.Information("Using Tesseract data path: {TesseractDataPath}", config.TesseractDataPath);
                Log.Information("Using Tesseract language: {TesseractLanguage}", config.TesseractLanguage);

                // Get directory path from command line args or prompt user
                string? path = null; // Declare the path variable here
                
                if (args.Length > 0)
                {
                    config.BasePath = args[0];
                    
                    // Save this path for next time
                    config.SaveLastUsedPath(config.BasePath);
                }
                else
                {
                    // If we have a valid LastUsedPath, use it directly without prompting
                    if (!string.IsNullOrEmpty(config.LastUsedPath) && Directory.Exists(config.LastUsedPath))
                    {
                        config.BasePath = config.LastUsedPath;
                        Log.Information("Automatically using last used directory path: {BasePath}", config.BasePath);
                    }
                    else
                    {
                        string? defaultPath = !string.IsNullOrEmpty(config.LastUsedPath) ? config.LastUsedPath : config.BasePath;
                        Log.Debug("Default path selection: LastUsedPath='{LastUsedPath}', BasePath='{BasePath}', Selected='{DefaultPath}'", 
                            config.LastUsedPath, config.BasePath, defaultPath);
                        
                        // Show prompt only if we don't have a valid LastUsedPath
                        string prompt = $"Enter directory path to scan (leave blank for default: '{config.BasePath}'): ";
                        
                        Log.Information(prompt);
                        path = Console.ReadLine();
                    }
                    
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        // Verify the path is valid
                        if (Directory.Exists(path))
                        {
                            config.BasePath = path;
                            Log.Debug("Using user-provided directory path: {BasePath}", config.BasePath);
                            
                            // Save this path for next time
                            config.SaveLastUsedPath(config.BasePath);
                        }
                        else
                        {
                            Log.Error("Directory not found: {Path}", path);
                            
                            // Fall back to last used path if available, or default
                            if (!string.IsNullOrEmpty(config.LastUsedPath) && Directory.Exists(config.LastUsedPath))
                            {
                                config.BasePath = config.LastUsedPath;
                                Log.Information("Using last used path instead: {Path}", config.BasePath);
                            }
                            else
                            {
                                Log.Debug("Using default directory path: {BasePath}", config.BasePath);
                            }
                        }
                    }
                    else
                    {
                        // We're using the default path since the user didn't provide a new one
                        Log.Debug("Using default directory path: {BasePath}", config.BasePath);
                    }
                }

                // Validate directory path
                if (!Directory.Exists(config.BasePath))
                {
                    Log.Error("Directory not found: {BasePath}", config.BasePath);
                    throw new DirectoryNotFoundException($"Directory not found: {config.BasePath}");
                }
                Log.Debug("Directory validated: {BasePath}", config.BasePath);

                // Log whether subdirectories will be included (from configuration)
                Log.Debug("Include subdirectories (from config): {IncludeSubdirectories}", config.IncludeSubdirectories);

                // Create file processors
                // Configure services using DI container
                var serviceProvider = ServiceConfiguration.ConfigureServices(config);
                
                // Resolve the main service from the container
                var fileService = serviceProvider.GetRequiredService<IFileService>();
                Log.Information("Starting file processing");
                await fileService.ProcessFilesAsync();

                Log.Information("Processing completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during processing: {ErrorMessage}", ex.Message);
            }
            finally
            {
                // Close and flush the log
                Log.CloseAndFlush();
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
