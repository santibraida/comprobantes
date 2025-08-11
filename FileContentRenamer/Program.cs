using FileContentRenamer.Models;
using FileContentRenamer.Services;
using Microsoft.Extensions.Configuration;
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
                Log.Debug("AppConfig initialized from configuration file");

                // Get directory path from command line args or prompt user
                if (args.Length > 0)
                {
                    config.BasePath = args[0];
                    Log.Debug("Using directory path from command line: {BasePath}", config.BasePath);
                    
                    // Save this path for next time
                    config.SaveLastUsedPath(config.BasePath);
                }
                else
                {
                    string? defaultPath = !string.IsNullOrEmpty(config.LastUsedPath) ? config.LastUsedPath : config.BasePath;
                    Log.Debug("Default path selection: LastUsedPath='{LastUsedPath}', BasePath='{BasePath}', Selected='{DefaultPath}'", 
                        config.LastUsedPath, config.BasePath, defaultPath);
                    
                    // Show the last used path in the prompt if available
                    string prompt = !string.IsNullOrEmpty(config.LastUsedPath) 
                        ? $"Enter directory path to scan (leave blank for last used path: '{config.LastUsedPath}'): "
                        : $"Enter directory path to scan (leave blank for default: '{config.BasePath}'): ";
                    
                    Log.Information(prompt);
                    string? path = Console.ReadLine();
                    
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
                        // Use last used path if available
                        if (!string.IsNullOrEmpty(config.LastUsedPath) && Directory.Exists(config.LastUsedPath))
                        {
                            config.BasePath = config.LastUsedPath;
                            Log.Information("Using last used directory path: {BasePath}", config.BasePath);
                        }
                        else
                        {
                            Log.Debug("Using default directory path: {BasePath}", config.BasePath);
                        }
                    }
                }

                // Validate directory path
                if (!Directory.Exists(config.BasePath))
                {
                    Log.Error("Directory not found: {BasePath}", config.BasePath);
                    throw new DirectoryNotFoundException($"Directory not found: {config.BasePath}");
                }
                Log.Debug("Directory validated: {BasePath}", config.BasePath);

                // Ask about subdirectories
                Log.Information("Include subdirectories? (Y/N, default: Y): ");
                string includeSubdirs = Console.ReadLine()?.ToUpperInvariant() ?? "Y";
                config.IncludeSubdirectories = includeSubdirs != "N";
                Log.Debug("Include subdirectories: {IncludeSubdirectories}", config.IncludeSubdirectories);

                // Create file processors
                var processors = new List<IFileProcessor>
                {
                    new PdfProcessor(),
                    new TextProcessor(),
                    new ImageProcessor(config)
                };
                Log.Debug("File processors initialized");

                // Create and run the file service
                IFileService fileService = new FileService(config, processors);
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

            Log.Information("Press any key to exit...");
            Console.ReadKey();
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
            
            // Configure Serilog with explicit paths
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "FileContentRenamer")
                .Enrich.WithProperty("Version", "1.0.0")
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("UserName", Environment.UserName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(logsPath, "app.log"),
                    rollingInterval: RollingInterval.Day,
                    shared: true,
                    fileSizeLimitBytes: null,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Application}/{MachineName}/{UserName}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
    }
}
