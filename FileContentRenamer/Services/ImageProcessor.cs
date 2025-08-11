using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FileContentRenamer.Models;
using Tesseract;
using Serilog;

namespace FileContentRenamer.Services
{
    public class ImageProcessor : IFileProcessor
    {
        private readonly AppConfig _config;
        private static bool _tesseractPathsInitialized = false;
        private bool _useLibrary = true; // Try library approach first, then fall back to command-line

        public ImageProcessor(AppConfig config)
        {
            _config = config;
            Log.Debug("ImageProcessor initialized");
            
            // Try to initialize Tesseract paths if not done already
            if (!_tesseractPathsInitialized)
            {
                try
                {
                    InitializeTesseractPaths();
                    _tesseractPathsInitialized = true;
                    Log.Debug("Tesseract paths initialized successfully");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to initialize Tesseract paths");
                }
            }
            
            // Verify Tesseract is available (checks both library and command-line)
            VerifyTesseractAvailability();
        }
        
        private void InitializeTesseractPaths()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Common Homebrew paths on macOS
                string[] possiblePaths = {
                    "/opt/homebrew/lib", 
                    "/usr/local/lib",
                    "/opt/homebrew/Cellar/leptonica/1.85.0/lib",
                    "/opt/homebrew/Cellar/tesseract/5.5.1/lib"
                };
                
                // Tell .NET where to look for native libraries
                string pathVar = Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH") ?? "";
                string newPath = string.Join(":", possiblePaths);
                if (!string.IsNullOrEmpty(pathVar))
                {
                    newPath = pathVar + ":" + newPath;
                }
                Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", newPath);
                
                Log.Debug("Set DYLD_LIBRARY_PATH to: {Path}", newPath);
            }
        }
        
        private void VerifyTesseractAvailability()
        {
            // Check if tessdata directory exists
            if (_config.TesseractDataPath != null && !Directory.Exists(_config.TesseractDataPath))
            {
                Log.Warning("Tessdata directory does not exist: {TessdataDir}. Creating it now.", _config.TesseractDataPath);
                try {
                    Directory.CreateDirectory(_config.TesseractDataPath);
                    Log.Information("Created tessdata directory at: {TessdataDir}", _config.TesseractDataPath);
                } 
                catch (Exception ex) {
                    Log.Error(ex, "Failed to create tessdata directory: {ErrorMessage}", ex.Message);
                }
            }
            else if (_config.TesseractDataPath != null)
            {
                Log.Information("Tessdata directory found at: {TessdataDir}", _config.TesseractDataPath);
            }
            
            // Also check for command-line availability
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "tesseract",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(startInfo);
                string output = process?.StandardOutput.ReadToEndAsync().Result ?? "";
                process?.WaitForExit();
                
                if (process != null && process.ExitCode == 0)
                {
                    string tesseractVersion = output.Split('\n').FirstOrDefault() ?? "Unknown version";
                    Log.Information("Tesseract command-line detected: {TesseractVersion}", tesseractVersion);
                }
                else
                {
                    Log.Warning("Tesseract command-line not found. Please install Tesseract OCR.");
                    // If command-line is not available, try to use library only
                    _useLibrary = true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not verify Tesseract command-line availability");
                // If command-line check fails, try to use library only
                _useLibrary = true;
            }
        }

        public bool CanProcess(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            bool canProcess = extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tif" || extension == ".tiff";
            
            // Log which files are identified as processable by this processor
            if (canProcess)
            {
                Log.Debug("ImageProcessor identified file for processing: {FilePath}", Path.GetFileName(filePath));
            }
            
            return canProcess;
        }

        public async Task<string> ExtractContentAsync(string filePath)
        {
            try
            {
                Log.Information("Attempting to extract text from image: {FilePath}", Path.GetFileName(filePath));
                
                // Check if tesseract data directory exists
                if (string.IsNullOrEmpty(_config.TesseractDataPath) || !Directory.Exists(_config.TesseractDataPath))
                {
                    string error = $"Tesseract data directory not found: {_config.TesseractDataPath}";
                    Log.Error("Tesseract data directory error: {Error}", error);
                    return string.Empty;
                }
                
                // First try using the Tesseract library approach
                if (_useLibrary)
                {
                    try
                    {
                        var libraryResult = await ExtractUsingLibraryAsync(filePath);
                        if (!string.IsNullOrEmpty(libraryResult))
                        {
                            return libraryResult;
                        }
                        // If we get here, the library approach failed but didn't throw
                        Log.Warning("Tesseract library approach returned empty result, falling back to command-line");
                        _useLibrary = false;
                    }
                    catch (Exception ex)
                    {
                        // Library approach failed with exception, log and fall back to command-line
                        Log.Warning(ex, "Tesseract library approach failed, falling back to command-line");
                        _useLibrary = false;
                    }
                }
                
                // If library approach is disabled or failed, use command-line approach
                return await ExtractUsingCommandLineAsync(filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to extract text from image: {FilePath}", Path.GetFileName(filePath));
                return string.Empty;
            }
        }
        
        private async Task<string> ExtractUsingLibraryAsync(string filePath)
        {
            if (string.IsNullOrEmpty(_config.TesseractLanguage) || string.IsNullOrEmpty(_config.TesseractDataPath))
            {
                Log.Error("Tesseract configuration is missing: Language={Lang}, DataPath={Path}", 
                    _config.TesseractLanguage, _config.TesseractDataPath);
                return string.Empty;
            }
            
            // Check if language data files exist
            string[] langFiles = _config.TesseractLanguage.Split('+');
            foreach (var lang in langFiles)
            {
                string langFile = Path.Combine(_config.TesseractDataPath, $"{lang}.traineddata");
                if (!File.Exists(langFile))
                {
                    string error = $"Tesseract language file not found: {langFile}";
                    Log.Error("Tesseract language error: {Error}", error);
                    return string.Empty;
                }
            }
            
            // Process image files using Tesseract OCR
            Log.Debug("Creating Tesseract engine with data path: {TesseractDataPath}", _config.TesseractDataPath);
            
            // Using Tesseract SDK with enhanced configuration
            using (var engine = new TesseractEngine(_config.TesseractDataPath, _config.TesseractLanguage))
            {
                // Configure engine for better quality
                engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,;:¡!¿?\"'()[]{}-_=+*/\\@#$%&|<> ");
                engine.SetVariable("debug_file", "/dev/null");
                engine.SetVariable("tessedit_do_invert", "0");
                engine.SetVariable("textord_heavy_nr", "1");  // Helps with dark backgrounds
                engine.SetVariable("textord_min_linesize", "2.5");  // Better detection of small text
                
                // Load the image
                Log.Debug("Loading image file: {FileName}", Path.GetFileName(filePath));
                using (var img = Pix.LoadFromFile(filePath))
                {
                    // Process the image with Tesseract OCR
                    Log.Debug("Processing image with Tesseract library");
                    using (var page = engine.Process(img))
                    {
                        // Get the OCR text result
                        string text = page.GetText();
                        
                        // Log the first part of the extracted text
                        string previewText = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
                        Log.Debug("Text extracted from image (preview): {PreviewText}", previewText);
                        
                        // Additional check for Quilmes specifically
                        if (previewText.ToLowerInvariant().Contains("quil"))
                        {
                            Log.Information("Detected potential Quilmes document");
                        }
                        
                        return await Task.FromResult(text);
                    }
                }
            }
        }
        
        private async Task<string> ExtractUsingCommandLineAsync(string filePath)
        {
            Log.Debug("Attempting to extract text using command-line Tesseract");
            
            // Create a temporary directory for the output
            string tempDir = Path.Combine(Path.GetTempPath(), "OCR_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            
            try
            {
                string outputBase = Path.Combine(tempDir, $"ocr_output_{DateTime.Now.Ticks}");
                string outputTxtFile = $"{outputBase}.txt";
                
                // First try running a command to get the available languages
                var langCheckStartInfo = new ProcessStartInfo
                {
                    FileName = "tesseract",
                    Arguments = "--list-langs",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                try
                {
                    Log.Debug("Checking available Tesseract languages");
                    using var langProcess = Process.Start(langCheckStartInfo);
                    if (langProcess != null)
                    {
                        var langOutput = new StringBuilder();
                        langOutput.AppendLine(langProcess.StandardOutput.ReadToEnd());
                        langOutput.AppendLine(langProcess.StandardError.ReadToEnd());
                        langProcess.WaitForExit();
                        
                        // Log available languages
                        Log.Debug("Available Tesseract languages: {Languages}", langOutput.ToString().Trim());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to list Tesseract languages");
                }
                
                // Prepare language parameter with fallback options
                string languageParam = _config.TesseractLanguage ?? "eng";
                
                // If language contains a plus (+), try with just the first language
                if (languageParam.Contains("+"))
                {
                    string primaryLang = languageParam.Split('+')[0];
                    Log.Debug("Using primary language: {PrimaryLanguage} (from {OriginalLanguage})", 
                        primaryLang, languageParam);
                    languageParam = primaryLang;
                }
                
                // Run tesseract command line directly
                var startInfo = new ProcessStartInfo
                {
                    FileName = "tesseract",
                    Arguments = $"\"{filePath}\" \"{outputBase}\" -l {languageParam}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                // Log the command
                Log.Debug("Running Tesseract command: {Command} {Arguments}", startInfo.FileName, startInfo.Arguments);
                
                // Run the process
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        
                        await process.WaitForExitAsync();
                        
                        if (process.ExitCode != 0)
                        {
                            Log.Error("Tesseract process failed with exit code {ExitCode}: {Error}", 
                                process.ExitCode, error);
                            return string.Empty;
                        }
                        
                        // Read the output file
                        if (File.Exists(outputTxtFile))
                        {
                            string text = await File.ReadAllTextAsync(outputTxtFile);
                            
                            // Log the first part of the extracted text
                            string previewText = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
                            Log.Debug("Text extracted from image using command-line (preview): {PreviewText}", previewText);
                            
                            return text;
                        }
                        else
                        {
                            Log.Error("Tesseract output file not found: {OutputFile}", outputTxtFile);
                            return string.Empty;
                        }
                    }
                    else
                    {
                        Log.Error("Failed to start Tesseract process");
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing Tesseract command line: {ErrorMessage}", ex.Message);
                return string.Empty;
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to clean up temporary directory: {TempDir}", tempDir);
                }
            }
        }
    }
}
