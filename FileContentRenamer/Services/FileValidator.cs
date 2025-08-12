using FileContentRenamer.Models;
using Serilog;

namespace FileContentRenamer.Services
{
    public class FileValidator : IFileValidator
    {
        private readonly AppConfig _config;
        private readonly IFilenameGenerator _filenameGenerator;

        public FileValidator(AppConfig config, IFilenameGenerator filenameGenerator)
        {
            _config = config;
            _filenameGenerator = filenameGenerator;
        }

        public bool ShouldProcessFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Log.Warning("File does not exist or path is invalid: {FilePath}", filePath);
                return false;
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            bool hasValidExtension = _config.FileExtensions?.Contains(extension) == true;
            
            if (!hasValidExtension)
            {
                Log.Debug("File extension {Extension} not in configured extensions list", extension);
                return false;
            }

            return true;
        }

        public bool ShouldSkipAlreadyNamedFile(string filePath)
        {
            if (_config.ForceReprocessAlreadyNamed)
                return false;

            string baseName = Path.GetFileNameWithoutExtension(filePath);
            
            return _filenameGenerator.IsAlreadyNamedFilename(baseName);
        }

        public bool ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(_config.BasePath))
            {
                Log.Error("Configuration error: BasePath is not set");
                return false;
            }

            if (!Directory.Exists(_config.BasePath))
            {
                Log.Error("Configuration error: BasePath does not exist: {BasePath}", _config.BasePath);
                return false;
            }

            if (_config.FileExtensions == null || _config.FileExtensions.Count == 0)
            {
                Log.Error("Configuration error: No file extensions configured");
                return false;
            }

            return true;
        }

        public void ValidateFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                const long maxSizeBytes = 50 * 1024 * 1024; // 50 MB
                
                if (fileInfo.Length > maxSizeBytes)
                {
                    Log.Warning("Large file detected: {FileName} ({SizeMB} MB)", 
                        Path.GetFileName(filePath), fileInfo.Length / (1024 * 1024));
                }
                else if (fileInfo.Length == 0)
                {
                    Log.Warning("Empty file detected: {FileName}", Path.GetFileName(filePath));
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not validate file size for: {FileName}", Path.GetFileName(filePath));
            }
        }
    }
}
