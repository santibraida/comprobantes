using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FileContentRenamer.Models
{
    public class AppConfig
    {
        // Making all properties nullable since we won't assume defaults
        public string? BasePath { get; set; }
        public string? LastUsedPath { get; set; } 
        public List<string>? FileExtensions { get; set; }
        public bool IncludeSubdirectories { get; set; } = true; // Only keeping this default
        public string? TesseractDataPath { get; set; }
        public string? TesseractLanguage { get; set; }
        public NamingRules NamingRules { get; set; } = new NamingRules();
        
        /// <summary>
        /// Loads configuration from the appsettings.json file - throws an exception if config file is missing or invalid
        /// </summary>
        public static AppConfig LoadFromConfiguration(string? basePath = null)
        {
            // Get the base directory for the application
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            
            // Find solution directory more robustly
            string solutionDir = FindSolutionDirectory(baseDir);
            
            // Check if appsettings.json exists in any of these locations
            string? configPath = FindConfigFile(baseDir, solutionDir);
            
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
                
                // Check if LastUsedPath exists and is valid
                if (!string.IsNullOrEmpty(appConfig.LastUsedPath) && Directory.Exists(appConfig.LastUsedPath))
                {
                    Log.Information("Found valid LastUsedPath in configuration: {LastUsedPath}", appConfig.LastUsedPath);
                }
                else if (!string.IsNullOrEmpty(appConfig.LastUsedPath))
                {
                    Log.Warning("LastUsedPath from configuration is invalid or doesn't exist: {LastUsedPath}", appConfig.LastUsedPath);
                    appConfig.LastUsedPath = string.Empty;
                }
                else
                {
                    Log.Debug("No LastUsedPath found in configuration");
                    appConfig.LastUsedPath = string.Empty;
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
                
                Log.Debug("Loaded configuration with LastUsedPath: {LastUsedPath}", appConfig.LastUsedPath);
                
                // Override with command-line provided base path if specified
                if (!string.IsNullOrEmpty(basePath))
                {
                    appConfig.BasePath = basePath;
                }
                
                // Ensure TesseractDataPath is an absolute path
                appConfig.TesseractDataPath = Path.GetFullPath(appConfig.TesseractDataPath);
                
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
        private static string FindSolutionDirectory(string baseDir)
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
            
            // If we can't find the solution directory, return the baseDir
            return baseDir;
        }
        
        /// <summary>
        /// Finds the configuration file in common locations
        /// </summary>
        private static string? FindConfigFile(string baseDir, string solutionDir)
        {
            string[] possibleLocations = new[] 
            {
                Path.Combine(baseDir, "appsettings.json"),
                Path.Combine(Directory.GetParent(baseDir)?.FullName ?? baseDir, "appsettings.json"),
                Path.Combine(solutionDir, "appsettings.json")
            };
            
            foreach (var location in possibleLocations) 
            {
                if (File.Exists(location))
                {
                    return location;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Saves the LastUsedPath configuration to appsettings.json
        /// </summary>
        public void SaveLastUsedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
                
            try
            {
                // Find the appsettings.json path using the same function used during loading
                string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
                
                // Find solution directory more robustly
                string solutionDir = FindSolutionDirectory(baseDir);
                
                // Use the same approach as LoadFromConfiguration to find the config file
                string? configPath = FindConfigFile(baseDir, solutionDir);
                
                // Also identify the root appsettings.json file
                string rootConfigPath = Path.Combine(solutionDir, "appsettings.json");
                
                // Log all attempts to locate the configuration file
                Log.Debug("Trying to find appsettings.json for saving LastUsedPath. Base dir: {BaseDir}, Solution dir: {SolutionDir}", baseDir, solutionDir);
                
                // If no config file was found, try to use the root config file
                if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                {
                    Log.Warning("Could not find appsettings.json to save LastUsedPath in application directory.");
                    
                    // Check if root config exists
                    if (File.Exists(rootConfigPath))
                    {
                        configPath = rootConfigPath;
                        Log.Information("Using root appsettings.json: {Path}", rootConfigPath);
                    }
                    else
                    {
                        // As a last resort, try the direct path to the known location
                        string directPath = Path.Combine("/Users/santibraida/Workspace/comprobantes", "appsettings.json");
                        if (File.Exists(directPath))
                        {
                            configPath = directPath;
                            Log.Information("Found appsettings.json at hardcoded path: {Path}", directPath);
                        }
                        else
                        {
                            Log.Error("Could not find appsettings.json even at the known location. Cannot save LastUsedPath.");
                            return;
                        }
                    }
                }
                
                Log.Debug("Using configuration file at: {ConfigPath} to save LastUsedPath", configPath);
                
                // Update the instance property
                LastUsedPath = path;
                
                // Read existing JSON
                string jsonContent = File.ReadAllText(configPath);
                
                // Parse JSON
                using JsonDocument doc = JsonDocument.Parse(jsonContent);
                
                // Create a new JSON document
                using var ms = new MemoryStream();
                using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
                
                writer.WriteStartObject();
                
                // Copy all elements from the root
                foreach (JsonProperty property in doc.RootElement.EnumerateObject())
                {
                    if (property.Name != "AppConfig")
                    {
                        // Copy non-AppConfig sections as-is
                        property.WriteTo(writer);
                    }
                    else
                    {
                        // For AppConfig, we need to modify the LastUsedPath
                        writer.WritePropertyName("AppConfig");
                        writer.WriteStartObject();
                        
                        // Copy all existing AppConfig properties
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
                                // Skip writing the old value as we'll write the new one below
                            }
                        }
                        
                        // Write the updated LastUsedPath
                        writer.WriteString("LastUsedPath", LastUsedPath);
                        writer.WriteEndObject();
                        
                        // Log whether we updated or added the property
                        if (lastUsedPathExists)
                            Log.Debug("Updated existing LastUsedPath in configuration");
                        else
                            Log.Debug("Added new LastUsedPath property to configuration");
                    }
                }
                
                writer.WriteEndObject();
                writer.Flush();
                
                // Get the JSON content as byte array
                byte[] jsonBytes = ms.ToArray();
                
                // Write the updated JSON back to the application config file
                File.WriteAllBytes(configPath, jsonBytes);
                Log.Information("LastUsedPath '{LastUsedPath}' successfully saved to configuration file: {ConfigPath}", 
                    LastUsedPath, configPath);
                
                // If the config file is different from the root config, also update the root config
                if (File.Exists(rootConfigPath) && !string.Equals(configPath, rootConfigPath, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.WriteAllBytes(rootConfigPath, jsonBytes);
                        Log.Information("LastUsedPath also saved to root configuration file: {RootConfigPath}", rootConfigPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Could not save LastUsedPath to root configuration file: {RootConfigPath}", rootConfigPath);
                    }
                }
                
                // Verify the file was actually updated
                try
                {
                    string verifyContent = File.ReadAllText(configPath);
                    if (verifyContent.Contains($"\"LastUsedPath\": \"{LastUsedPath.Replace("\\", "\\\\")}\""))
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
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save LastUsedPath configuration: {ErrorMessage}", ex.Message);
            }
        }
        
    }
}
