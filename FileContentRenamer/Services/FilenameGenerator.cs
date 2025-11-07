using System.Text.RegularExpressions;
using FileContentRenamer.Models;
using Serilog;

namespace FileContentRenamer.Services
{
    public partial class FilenameGenerator : IFilenameGenerator
    {
        private readonly IDateExtractor _dateExtractor;
        private readonly AppConfig _config;
        private static readonly object _filenameLock = new();

        // Compiled regex patterns for better performance
        private static readonly Regex FilenameValidationRegex = new(@"^[a-zA-Z0-9_]+_\d{4}-\d{2}-\d{2}_[a-zA-Z0-9_]+$", RegexOptions.Compiled);
        private static readonly Regex MultipleUnderscoresRegex = new(@"_{2,}", RegexOptions.Compiled);

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
            return FilenameValidationRegex.IsMatch(filenameWithoutExtension);
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

                // Pre-compile the regex pattern for this filename
                var numberingRegex = new Regex(@"^" + Regex.Escape(filenameWithoutExtension) + @"_(\d+)$", RegexOptions.Compiled);

                // Check for existing numbered versions
                var existingFiles = Directory.GetFiles(directory, $"{filenameWithoutExtension}*{extension}")
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToList();

                // Find the highest number among numbered files
                int highestNumber = existingFiles
                    .Where(existingFile => existingFile != null)
                    .Select(existingFile =>
                    {
                        var match = numberingRegex.Match(existingFile!);
                        return match.Success && int.TryParse(match.Groups[1].Value, out int number) ? number : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                // If base file exists but no numbered versions, start at 2
                // Otherwise, increment the highest number found
                int nextNumber = highestNumber == 0 ? 2 : highestNumber + 1;
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
            filename = MultipleUnderscoresRegex.Replace(filename, "_");

            // Remove leading/trailing underscores
            filename = filename.Trim('_');

            // Ensure it's not empty
            if (string.IsNullOrEmpty(filename))
                filename = "cleaned_file";

            return filename;
        }
    }
}
