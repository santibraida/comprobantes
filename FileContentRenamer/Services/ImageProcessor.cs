using System.Diagnostics;
using System.Text;
using FileContentRenamer.Models;
using Serilog;

namespace FileContentRenamer.Services
{
    public class ImageProcessor : IFileProcessor
    {
        private readonly AppConfig _config;

        public ImageProcessor(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            VerifyTesseractAvailability();
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
            
            // Check for command-line availability
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
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not verify Tesseract command-line availability");
            }
        }

        public bool CanProcess(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            bool canProcess = extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tif" || extension == ".tiff";
            
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
                
                // Use command-line approach only
                return await ExtractUsingCommandLineAsync(filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to extract text from image: {FilePath}", Path.GetFileName(filePath));
                return string.Empty;
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
                    using var langProcess = Process.Start(langCheckStartInfo);
                    if (langProcess != null)
                    {
                        var langOutput = new StringBuilder();
                        langOutput.AppendLine(langProcess.StandardOutput.ReadToEnd());
                        langOutput.AppendLine(langProcess.StandardError.ReadToEnd());
                        langProcess.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to list Tesseract languages");
                }
                
                // Prepare language parameter with fallback options
                string languageParam = _config.TesseractLanguage ?? "eng";
                
                // If language contains a plus (+), try with just the first language
                if (languageParam.Contains('+'))
                {
                    string primaryLang = languageParam.Split('+')[0];
                    languageParam = primaryLang;
                }
                
                // Run tesseract command line directly
                var startInfo = new ProcessStartInfo
                {
                    FileName = "tesseract",
                    Arguments = $"\"{filePath}\" \"{outputBase}\" -l {languageParam} --tessdata-dir \"{_config.TesseractDataPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
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
                            return text ?? string.Empty;
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
