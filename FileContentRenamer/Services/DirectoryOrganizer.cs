using System.Text.RegularExpressions;
using FileContentRenamer.Models;
using Serilog;

namespace FileContentRenamer.Services
{
    public class DirectoryOrganizer : IDirectoryOrganizer
    {
        private static readonly Dictionary<int, string> MonthNames = new Dictionary<int, string>
        {
            { 1, "enero" }, { 2, "febrero" }, { 3, "marzo" }, { 4, "abril" },
            { 5, "mayo" }, { 6, "junio" }, { 7, "julio" }, { 8, "agosto" },
            { 9, "septiembre" }, { 10, "octubre" }, { 11, "noviembre" }, { 12, "diciembre" }
        };

        private static readonly object _directoryLock = new object();
        private readonly string _basePath;

        public DirectoryOrganizer(AppConfig config)
        {
            _basePath = config.BasePath ?? ".";
        }

        public string GetMonthName(int monthNumber)
        {
            return MonthNames.TryGetValue(monthNumber, out string? name) ? name : "unknown";
        }

        public bool IsInYearMonthStructure(string directoryPath)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
                DirectoryInfo? parentDir = dirInfo.Parent;

                // Check if current directory is a month folder (format: MM_monthname)
                bool isMonthFolder = Regex.IsMatch(dirInfo.Name, @"^\d{2}_\w+$");

                // Check if parent directory is a year folder (format: yyyy)
                bool parentIsYearFolder = parentDir != null && Regex.IsMatch(parentDir.Name, @"^\d{4}$");

                // Check if current directory is a year folder
                bool isYearFolder = Regex.IsMatch(dirInfo.Name, @"^\d{4}$");

                return (isMonthFolder && parentIsYearFolder) || isYearFolder;
            }
            catch
            {
                return false;
            }
        }

        public string OrganizeFileIntoDirectoryStructure(string filePath, string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr) || !DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime fileDate))
            {
                return filePath; // Return original path if date parsing fails
            }

            string? directory = Path.GetDirectoryName(filePath);
            if (directory == null)
            {
                Log.Warning("Could not determine directory for file: {FilePath}", filePath);
                return filePath;
            }

            string filename = Path.GetFileName(filePath);
            string yearFolder = fileDate.Year.ToString();
            string monthFolder = $"{fileDate.Month:D2}_{GetMonthName(fileDate.Month)}";

            // Check if we're already in the correct year/month structure
            if (IsInYearMonthStructure(directory))
            {
                DirectoryInfo currentDir = new DirectoryInfo(directory);

                // Check if we're in the correct month folder
                if (currentDir.Name == monthFolder && currentDir.Parent?.Name == yearFolder)
                {
                    return filePath; // Already in correct location
                }

                // We need to move to a different month/year folder
                // Use the configured base path to ensure we stay within the original scan directory
                return MoveToYearMonthStructure(filePath, _basePath, yearFolder, monthFolder);
            }

            // Not in year/month structure, need to organize
            // Always use the configured base path to ensure year folders are created in the correct location
            string yearPath = Path.Combine(_basePath, yearFolder);
            if (!Directory.Exists(yearPath))
            {
                Log.Information("Creating year folder: {YearFolder}", yearPath);
                lock (_directoryLock)
                {
                    Directory.CreateDirectory(yearPath);
                }
            }

            string monthPath = Path.Combine(yearPath, monthFolder);
            if (!Directory.Exists(monthPath))
            {
                Log.Information("Creating target month folder: {MonthFolder}", monthPath);
                lock (_directoryLock)
                {
                    Directory.CreateDirectory(monthPath);
                }
            }

            string targetPath = Path.Combine(monthPath, filename);

            // Check if we need to move the file
            if (!string.Equals(filePath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Moving file to organized structure: {Source} -> {Target}",
                    Path.GetFileName(filePath), Path.GetRelativePath(directory, targetPath));

                // Use lock to prevent race conditions when moving files
                lock (_directoryLock)
                {
                    targetPath = EnsureUniqueFilename(targetPath);
                    File.Move(filePath, targetPath);
                }
                return targetPath;
            }

            return filePath;
        }

        private static string MoveToYearMonthStructure(string currentPath, string basePath, string yearFolder, string monthFolder)
        {
            string filename = Path.GetFileName(currentPath);
            string yearPath = Path.Combine(basePath, yearFolder);
            string monthPath = Path.Combine(yearPath, monthFolder);

            // Create directories if they don't exist
            if (!Directory.Exists(yearPath))
            {
                Log.Information("Creating year folder: {YearFolder}", yearPath);
                lock (_directoryLock)
                {
                    Directory.CreateDirectory(yearPath);
                }
            }

            if (!Directory.Exists(monthPath))
            {
                Log.Information("Creating target month folder: {MonthFolder}", monthPath);
                lock (_directoryLock)
                {
                    Directory.CreateDirectory(monthPath);
                }
            }

            string targetPath = Path.Combine(monthPath, filename);

            // Check if we need to move the file
            if (!string.Equals(currentPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                string currentFolder = Path.GetFileName(Path.GetDirectoryName(currentPath) ?? "");
                Log.Information("Moving file from {CurrentFolder} to {TargetFolder} folder",
                    currentFolder, monthFolder);

                // Use lock to prevent race conditions when moving files
                lock (_directoryLock)
                {
                    targetPath = EnsureUniqueFilename(targetPath);
                    File.Move(currentPath, targetPath);
                }
                return targetPath;
            }

            return currentPath;
        }

        private static string EnsureUniqueFilename(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            string directory = Path.GetDirectoryName(filePath) ?? "";
            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 2;
            string newFilePath;
            do
            {
                string newFilename = $"{filenameWithoutExtension}_{counter}{extension}";
                newFilePath = Path.Combine(directory, newFilename);
                counter++;
            }
            while (File.Exists(newFilePath));

            return newFilePath;
        }
    }
}
