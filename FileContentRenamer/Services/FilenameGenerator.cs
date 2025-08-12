using System.Text.RegularExpressions;
using FileContentRenamer.Models;
using Serilog;

namespace FileContentRenamer.Services
{
    public class FilenameGenerator : IFilenameGenerator
    {
        private readonly IDateExtractor _dateExtractor;
        private readonly AppConfig _config;
        private static readonly object _filenameLock = new object();

        public FilenameGenerator(IDateExtractor dateExtractor, AppConfig config)
        {
            _dateExtractor = dateExtractor;
            _config = config;
        }

        public string GenerateFilename(string content, string originalPath)
        {
            string? originalDirectory = Path.GetDirectoryName(originalPath);
            if (originalDirectory == null)
            {
                Log.Warning("Could not determine directory for file: {FilePath}", originalPath);
                return Path.GetFileName(originalPath);
            }

            string fileExtension = Path.GetExtension(originalPath);
            
            // Extract date from content
            string date = _dateExtractor.ExtractDateFromContent(content);
            if (string.IsNullOrEmpty(date))
            {
                Log.Warning("Could not extract date from content for file: {FileName}", Path.GetFileName(originalPath));
                date = DateTime.Now.ToString("yyyy-MM-dd");
            }

            // Generate filename using naming rules
            string generatedName = _config.NamingRules.GenerateFilename(content, date);
            
            // Clean up the filename
            generatedName = CleanupFileName(generatedName);
            
            return $"{generatedName}{fileExtension}";
        }

        public bool IsAlreadyNamedFilename(string filenameWithoutExtension)
        {
            // Check if filename follows the pattern: service_yyyy-MM-dd_payment
            return Regex.IsMatch(filenameWithoutExtension, @"^[a-zA-Z0-9_]+_\d{4}-\d{2}-\d{2}_[a-zA-Z0-9_]+$");
        }

        public string GenerateUniqueFilename(string directory, string baseFilename)
        {
            // Use lock to prevent race conditions when multiple threads check/create files
            lock (_filenameLock)
            {
                string targetPath = Path.Combine(directory, baseFilename);
                
                if (!File.Exists(targetPath))
                    return targetPath;

                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(baseFilename);
                string extension = Path.GetExtension(baseFilename);

                // Check for existing numbered versions
                var existingFiles = Directory.GetFiles(directory, $"{filenameWithoutExtension}*{extension}")
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToList();

                // Find the highest number
                int highestNumber = 1;
                foreach (var existingFile in existingFiles)
                {
                    if (existingFile != null)
                    {
                        var match = Regex.Match(existingFile, @"^" + Regex.Escape(filenameWithoutExtension) + @"_(\d+)$");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                        {
                            highestNumber = Math.Max(highestNumber, number);
                        }
                    }
                }

                // Generate next available filename
                int nextNumber = highestNumber + 1;
                string newFilename = $"{filenameWithoutExtension}_{nextNumber}{extension}";
                return Path.Combine(directory, newFilename);
            }
        }

        private static string CleanupFileName(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "unknown_file";

            // Remove invalid file name characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                filename = filename.Replace(invalidChar, '_');
            }

            // Replace multiple underscores with single underscore
            filename = Regex.Replace(filename, @"_{2,}", "_");
            
            // Remove leading/trailing underscores
            filename = filename.Trim('_');
            
            // Ensure it's not empty
            if (string.IsNullOrEmpty(filename))
                filename = "cleaned_file";

            return filename;
        }
    }
}
