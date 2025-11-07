using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FileContentRenamer.Models
{
    public class AppConfig : IAppConfig
    {
        private const string AppSettingsFileName = "appsettings.json";
        private static IConfigurationFileProvider _fileProvider = new ConfigurationFileProvider();

        // Making all properties nullable since we won't assume defaults
        public string? BasePath { get; set; }
        public string? LastUsedPath { get; set; }
        public List<string>? FileExtensions { get; set; }
        public bool IncludeSubdirectories { get; set; } = true; // Only keeping this default
        public string? TesseractDataPath { get; set; }
        public string? TesseractLanguage { get; set; }
        public bool ForceReprocessAlreadyNamed { get; set; } = false;
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        public NamingRules NamingRules { get; set; } = new NamingRules();

        /// <summary>
        /// Sets the file provider for testing purposes
        /// </summary>
        internal static void SetFileProvider(IConfigurationFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        /// <summary>
        /// Resets the file provider to the default implementation
        /// </summary>
        internal static void ResetFileProvider()
        {
            _fileProvider = new ConfigurationFileProvider();
        }

        /// <summary>
        /// Loads configuration from the appsettings.json file - throws an exception if config file is missing or invalid
        /// </summary>
        public static AppConfig LoadFromConfiguration(string? basePath = null)
        {
            return LoadFromConfiguration(basePath, _fileProvider);
        }

        /// <summary>
        /// Loads configuration from the appsettings.json file with injectable file provider - for testing
        /// </summary>
        internal static AppConfig LoadFromConfiguration(string? basePath, IConfigurationFileProvider fileProvider)
        {
            // Get the base directory for the application
            string baseDir = fileProvider.GetBaseDirectory();

            // Find solution directory more robustly
            string solutionDir = FindSolutionDirectory(baseDir, fileProvider);

            // Check if appsettings.json exists in any of these locations
            string? configPath = FindConfigFile(baseDir, solutionDir, fileProvider);

            // Throw exception if configuration file doesn't exist
            if (string.IsNullOrEmpty(configPath))
            {
                throw new FileNotFoundException("Configuration file appsettings.json not found. This file is required.");
            }

            Log.Debug("Found configuration file at: {ConfigPath}", configPath);

            try
            {
                // Build configuration
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(configPath) ?? baseDir)
                    .AddJsonFile(Path.GetFileName(configPath), optional: false, reloadOnChange: true);

                IConfiguration configuration = configBuilder.Build();

                // Create a new instance with no defaults
                var appConfig = new AppConfig();

                // Bind configuration to the AppConfig instance
                configuration.GetSection("AppConfig").Bind(appConfig);

                // Extract NamingRules default values from configuration
                var namingRulesSection = configuration.GetSection("AppConfig:NamingRules");
                string defaultServiceName = namingRulesSection["DefaultServiceName"] ?? "servicio";
                string defaultPaymentMethod = namingRulesSection["DefaultPaymentMethod"] ?? "santander";

                // Explicitly set NamingRules with default values from configuration
                appConfig.NamingRules = new NamingRules(defaultServiceName, defaultPaymentMethod);

                // Rebind rules collection that would have been lost in the new instance creation
                namingRulesSection.GetSection("Rules").Bind(appConfig.NamingRules.Rules);

                // Check if LastUsedPath exists and is valid
                if (!string.IsNullOrEmpty(appConfig.LastUsedPath))
                {
                    if (fileProvider.DirectoryExists(appConfig.LastUsedPath))
                    {
                        Log.Information("Found valid LastUsedPath in configuration: {LastUsedPath}", appConfig.LastUsedPath);
                    }
                    else
                    {
                        Log.Warning("LastUsedPath from configuration is invalid or doesn't exist: {LastUsedPath}", appConfig.LastUsedPath);
                        // Keep the path even if directory doesn't exist - it might be temporarily unavailable
                    }
                }
                else
                {
                    Log.Debug("No LastUsedPath found in configuration");
                }

                // Validate that required settings are present
                if (string.IsNullOrEmpty(appConfig.BasePath))
                {
                    throw new InvalidOperationException("BasePath is missing in configuration file");
                }

                if (appConfig.FileExtensions == null || appConfig.FileExtensions.Count == 0)
                {
                    throw new InvalidOperationException("FileExtensions is missing or empty in configuration file");
                }

                if (string.IsNullOrEmpty(appConfig.TesseractDataPath))
                {
                    throw new InvalidOperationException("TesseractDataPath is missing in configuration file");
                }

                if (string.IsNullOrEmpty(appConfig.TesseractLanguage))
                {
                    throw new InvalidOperationException("TesseractLanguage is missing in configuration file");
                }

                // Validate NamingRules default values are set
                if (string.IsNullOrEmpty(appConfig.NamingRules.DefaultServiceName))
                {
                    throw new InvalidOperationException("DefaultServiceName is missing in NamingRules configuration");
                }

                if (string.IsNullOrEmpty(appConfig.NamingRules.DefaultPaymentMethod))
                {
                    throw new InvalidOperationException("DefaultPaymentMethod is missing in NamingRules configuration");
                }

                Log.Debug("Loaded configuration with LastUsedPath: {LastUsedPath}", appConfig.LastUsedPath);
                Log.Debug("Loaded NamingRules with DefaultServiceName: {DefaultServiceName}, DefaultPaymentMethod: {DefaultPaymentMethod}",
                    appConfig.NamingRules.DefaultServiceName, appConfig.NamingRules.DefaultPaymentMethod);

                // Override with command-line provided base path if specified
                if (!string.IsNullOrEmpty(basePath))
                {
                    appConfig.BasePath = basePath;
                }

                // Ensure TesseractDataPath is an absolute path
                appConfig.TesseractDataPath = fileProvider.GetFullPath(appConfig.TesseractDataPath);

                return appConfig;
            }
            catch (Exception ex)
            {
                // Rethrow with more context
                throw new InvalidOperationException($"Failed to load configuration from {configPath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Finds the solution directory by looking for markers
        /// </summary>
        private static string FindSolutionDirectory(string baseDir, IConfigurationFileProvider fileProvider)
        {
            // Try to find the solution directory by traversing up from the current directory
            var currentDir = new DirectoryInfo(baseDir);
            while (currentDir != null)
            {
                // Check for markers that would indicate this is the solution root
                if (fileProvider.FileExists(Path.Combine(currentDir.FullName, "comprobantes.sln")) ||
                    (fileProvider.DirectoryExists(Path.Combine(currentDir.FullName, "FileContentRenamer")) &&
                     fileProvider.DirectoryExists(Path.Combine(currentDir.FullName, "tessdata"))))
                {
                    return currentDir.FullName;
                }

                // Move up one directory level
                currentDir = currentDir.Parent;
            }

            // If we can't find the solution directory, return the baseDir
            return baseDir;
        }

        /// <summary>
        /// Finds the configuration file in common locations
        /// </summary>
        private static string? FindConfigFile(string baseDir, string solutionDir, IConfigurationFileProvider fileProvider)
        {
            string[] possibleLocations =
            [
                Path.Combine(baseDir, AppSettingsFileName),
                Path.Combine(fileProvider.GetParentDirectory(baseDir) ?? baseDir, AppSettingsFileName),
                Path.Combine(solutionDir, AppSettingsFileName)
            ];

            return possibleLocations.FirstOrDefault(fileProvider.FileExists);
        }

        /// <summary>
        /// Saves the LastUsedPath configuration to appsettings.json
        /// </summary>
        public void SaveLastUsedPath(string path)
        {
            SaveLastUsedPath(path, _fileProvider);
        }

        /// <summary>
        /// Saves the LastUsedPath configuration to appsettings.json with injectable file provider - for testing
        /// </summary>
        internal void SaveLastUsedPath(string path, IConfigurationFileProvider fileProvider)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                // Find the appsettings.json path using the same function used during loading
                string baseDir = fileProvider.GetBaseDirectory();

                // Find solution directory more robustly
                string solutionDir = FindSolutionDirectory(baseDir, fileProvider);

                // Use the same approach as LoadFromConfiguration to find the config file
                string? configPath = FindConfigFile(baseDir, solutionDir, fileProvider);

                // Also identify the root appsettings.json file
                string rootConfigPath = Path.Combine(solutionDir, AppSettingsFileName);

                // Log all attempts to locate the configuration file
                Log.Debug("Trying to find {FileName} for saving LastUsedPath. Base dir: {BaseDir}, Solution dir: {SolutionDir}",
                    AppSettingsFileName, baseDir, solutionDir);

                // Determine which config file to use
                configPath = DetermineConfigPath(configPath, rootConfigPath, fileProvider);
                if (configPath == null)
                {
                    return;
                }

                // Update the configuration file
                UpdateConfigurationFile(configPath, path, fileProvider);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving LastUsedPath to configuration file");
            }
        }

        private static string? DetermineConfigPath(string? configPath, string rootConfigPath, IConfigurationFileProvider fileProvider)
        {
            // If no config file was found, try to use the root config file
            if (string.IsNullOrEmpty(configPath) || !fileProvider.FileExists(configPath))
            {
                Log.Warning("Could not find {FileName} to save LastUsedPath in application directory.", AppSettingsFileName);

                // Check if root config exists
                if (fileProvider.FileExists(rootConfigPath))
                {
                    Log.Information("Using root {FileName}: {Path}", AppSettingsFileName, rootConfigPath);
                    return rootConfigPath;
                }

                Log.Error("Could not find {FileName} in expected locations. Cannot save LastUsedPath.", AppSettingsFileName);
                return null;
            }

            Log.Debug("Using configuration file at: {ConfigPath} to save LastUsedPath", configPath);
            return configPath;
        }

        private void UpdateConfigurationFile(string configPath, string path, IConfigurationFileProvider fileProvider)
        {
            try
            {
                // Update the instance property
                LastUsedPath = path;

                // Read and parse JSON
                string jsonContent = fileProvider.ReadAllText(configPath);
                using JsonDocument doc = JsonDocument.Parse(jsonContent);

                // Create updated JSON
                byte[] jsonBytes = CreateUpdatedJson(doc);

                // Write to config file
                fileProvider.WriteAllBytes(configPath, jsonBytes);
                Log.Information("LastUsedPath '{LastUsedPath}' successfully saved to configuration file: {ConfigPath}",
                    LastUsedPath, configPath);

                // Update root config if needed
                UpdateRootConfigIfNeeded(configPath, jsonBytes, fileProvider);

                // Verify the update
                VerifyConfigUpdate(configPath, fileProvider);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save LastUsedPath configuration: {ErrorMessage}", ex.Message);
            }
        }

        private byte[] CreateUpdatedJson(JsonDocument doc)
        {
            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();

            // Copy all elements from the root
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                if (property.Name != "AppConfig")
                {
                    property.WriteTo(writer);
                }
                else
                {
                    WriteUpdatedAppConfig(writer, property);
                }
            }

            writer.WriteEndObject();
            writer.Flush();

            return ms.ToArray();
        }

        private void WriteUpdatedAppConfig(Utf8JsonWriter writer, JsonProperty property)
        {
            writer.WritePropertyName("AppConfig");
            writer.WriteStartObject();

            bool lastUsedPathExists = false;
            foreach (JsonProperty configProp in property.Value.EnumerateObject())
            {
                if (configProp.Name != "LastUsedPath")
                {
                    configProp.WriteTo(writer);
                }
                else
                {
                    lastUsedPathExists = true;
                }
            }

            writer.WriteString("LastUsedPath", LastUsedPath);
            writer.WriteEndObject();

            Log.Debug("{Action} LastUsedPath in configuration",
                lastUsedPathExists ? "Updated existing" : "Added new");
        }

        private static void UpdateRootConfigIfNeeded(string configPath, byte[] jsonBytes, IConfigurationFileProvider fileProvider)
        {
            string baseDir = fileProvider.GetBaseDirectory();
            string solutionDir = FindSolutionDirectory(baseDir, fileProvider);
            string rootConfigPath = Path.Combine(solutionDir, AppSettingsFileName);

            if (fileProvider.FileExists(rootConfigPath) &&
                !string.Equals(configPath, rootConfigPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    fileProvider.WriteAllBytes(rootConfigPath, jsonBytes);
                    Log.Information("LastUsedPath also saved to root configuration file: {RootConfigPath}", rootConfigPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not save LastUsedPath to root configuration file: {RootConfigPath}", rootConfigPath);
                }
            }
        }

        private void VerifyConfigUpdate(string configPath, IConfigurationFileProvider fileProvider)
        {
            try
            {
                string verifyContent = fileProvider.ReadAllText(configPath);
                if (verifyContent.Contains($"\"LastUsedPath\": \"{LastUsedPath?.Replace("\\", "\\\\")}\""))
                {
                    Log.Debug("Verified LastUsedPath was properly written to the configuration file");
                }
                else
                {
                    Log.Warning("LastUsedPath might not have been correctly saved to the configuration file");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error while verifying LastUsedPath was saved");
            }
        }

    }
}
