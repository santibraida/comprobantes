using System.Text.RegularExpressions;
using FileContentRenamer.Models;
using Serilog;

namespace FileContentRenamer.Services
{
    public class FileService : IFileService
    {
        private readonly AppConfig _config;
        private readonly List<IFileProcessor> _processors;

        public FileService(AppConfig config, List<IFileProcessor> processors)
        {
            _config = config;
            _processors = processors;
            Log.Debug("FileService initialized with {ProcessorCount} processors", processors.Count);
        }

        public async Task ProcessFilesAsync()
        {
            if (string.IsNullOrEmpty(_config.BasePath) || _config.FileExtensions == null)
            {
                Log.Error("Invalid configuration: BasePath={BasePath}, FileExtensions={FileExtensions}", 
                    _config.BasePath, _config.FileExtensions);
                return;
            }
            
            Log.Information("Starting to process files in {BasePath}", _config.BasePath);
            
            // Get all files with the specified extensions
            var searchOption = _config.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            Log.Debug("Searching for files with extensions: {Extensions}", string.Join(", ", _config.FileExtensions));
            var files = _config.FileExtensions
                .SelectMany(ext => Directory.GetFiles(_config.BasePath, $"*{ext}", searchOption))
                .ToList();

            Log.Information("Found {FileCount} files to process", files.Count);

            int processedCount = 0;
            foreach (var filePath in files)
            {
                await ProcessFileAsync(filePath);
                processedCount++;
                
                if (processedCount % 10 == 0)
                {
                    Log.Information("Processed {ProcessedCount}/{TotalCount} files", processedCount, files.Count);
                }
            }
            
            Log.Information("Finished processing {FileCount} files", files.Count);
        }

        private async Task ProcessFileAsync(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                Log.Information("Processing file: {FileName}", fileName);
                
                var fileType = Path.GetExtension(filePath).ToLowerInvariant();
                Log.Debug("File type: {FileType}", fileType);

                // Find the appropriate processor for this file type
                var processor = _processors.FirstOrDefault(p => p.CanProcess(filePath));
                if (processor == null)
                {
                    Log.Error("No processor found for file {FilePath}", filePath);
                    return;
                }
                
                Log.Debug("Using processor {ProcessorType} for file {FileName}", processor.GetType().Name, fileName);

                // Extract content from the file
                Log.Debug("Extracting content from {FilePath}", filePath);
                string content = await processor.ExtractContentAsync(filePath);
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    Log.Warning("No content extracted from {FilePath}", filePath);
                    return;
                }
                
                Log.Debug("Successfully extracted {ContentLength} characters from {FilePath}", 
                    content.Length, filePath);

                // Generate a new filename based on content
                Log.Debug("Generating filename for {FilePath}", filePath);
                
                // Generate filename using the configured rules
                string date = ExtractDateFromContent(content);
                string newFilename = _config.NamingRules.GenerateFilename(content, date);
                Log.Debug("Generated filename using naming rules: {NewFilename}", newFilename);
                
                if (string.IsNullOrWhiteSpace(newFilename))
                {
                    Log.Error("Could not generate a valid filename for {FilePath}", filePath);
                    return;
                }
                
                Log.Debug("Generated new filename: {NewFilename}", newFilename);

                // Rename the file
                string? directory = Path.GetDirectoryName(filePath);
                string extension = Path.GetExtension(filePath);
                
                // Ensure directory is not null (addresses CS8604 warning)
                if (directory == null)
                {
                    Log.Error("Could not determine directory for {FilePath}", filePath);
                    return;
                }
                
                // Extract date information for folder organization
                string dateStr = ExtractDateFromFilename(newFilename);
                
                // Define target directory - either current or year/month structure
                string targetDirectory = directory;
                
                // Parse the date to determine year/month folders
                if (DateTime.TryParse(dateStr, out DateTime fileDate))
                {
                    // Create year and month folder names
                    string yearFolder = fileDate.Year.ToString();
                    string monthFolder = $"{fileDate.Month:D2}_{GetMonthName(fileDate.Month)}";
                    
                    // Check if we're already in a year/month subfolder structure
                    if (!IsInYearMonthStructure(directory))
                    {
                        // Create or use year/month subdirectories
                        string yearPath = Path.Combine(directory, yearFolder);
                        string monthPath = Path.Combine(yearPath, monthFolder);
                        
                        // Create the directories if they don't exist
                        if (!Directory.Exists(yearPath))
                        {
                            Log.Information("Creating year folder: {YearFolder}", yearPath);
                            Directory.CreateDirectory(yearPath);
                        }
                        
                        if (!Directory.Exists(monthPath))
                        {
                            Log.Information("Creating month folder: {MonthFolder}", monthPath);
                            Directory.CreateDirectory(monthPath);
                        }
                        
                        targetDirectory = monthPath;
                        Log.Debug("File will be moved to year/month folder: {TargetDirectory}", targetDirectory);
                    }
                    else
                    {
                        Log.Debug("File already in year/month folder structure: {Directory}", directory);
                    }
                }
                
                string newPath = Path.Combine(targetDirectory, newFilename + extension);

                // If the new path is the same as the old one, no need to rename
                if (newPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("File already has an appropriate name: {FileName}", Path.GetFileName(filePath));

                    return;
                }

                // Make sure we don't overwrite an existing file
                if (File.Exists(newPath))
                {
                    Log.Debug("File name conflict detected for {NewPath}", newPath);
                    
                    // Generate a truly unique name with a counter approach
                    int counter = 1;
                    string baseFileName = newFilename;
                    string uniquePath;
                    
                    do {
                        uniquePath = Path.Combine(directory, $"{baseFileName}_{counter}{extension}");
                        counter++;
                    } while (File.Exists(uniquePath));
                    
                    newPath = uniquePath;
                    Log.Debug("Using unique name to avoid conflict: {NewFileName}", Path.GetFileName(newPath));
                }

                Log.Information("Renaming: {OldFileName} -> {NewFileName}", 
                    Path.GetFileName(filePath), Path.GetFileName(newPath));
                
                File.Move(filePath, newPath);

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing {FilePath}", filePath);

            }
        }

        private string GenerateFilename(string content, string originalPath)
        {
            try
            {
                // Clean up content - remove unnecessary spaces, line breaks, etc.
                content = content.Trim();
                
                // Extract date from the content or use today's date
                string date = ExtractDateFromContent(content);
                
                // Use the configuration-based naming rules
                string newFilename = _config.NamingRules.GenerateFilename(content, date);
                
                // If we couldn't generate a filename, use the first few words of the content
                if (string.IsNullOrWhiteSpace(newFilename))
                {
                    // Take the first 5-10 words
                    var words = content.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length > 2)  // Filter out very short words
                        .Take(5);
                    
                    newFilename = string.Join("_", words);
                }
                
                // Clean up the new filename
                newFilename = CleanupFileName(newFilename);
                
                // If still empty, use original name plus timestamp
                if (string.IsNullOrWhiteSpace(newFilename))
                {
                    newFilename = Path.GetFileNameWithoutExtension(originalPath) + "_" + DateTime.Now.ToString("yyyyMMdd");
                }
                
                return newFilename;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating filename for {FilePath}", originalPath);
                // On any error, return original name
                return Path.GetFileNameWithoutExtension(originalPath);
            }
        }
        
        /// <summary>
        /// Extracts a date from content or uses the current date if none is found
        /// </summary>
        private string ExtractDateFromContent(string content)
        {
            try
            {
                // Try to find dates (various formats)
                // First look for due dates (vencimiento)
                var dueDateMatch = Regex.Match(content, @"(?:vencimiento|vence|vto\.?)[:\s]*(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{2,4}|\d{2,4}[-/\.]\d{1,2}[-/\.]\d{1,2})", RegexOptions.IgnoreCase);
                if (dueDateMatch.Success)
                {
                    // Format the due date consistently
                    return StandardizeDate(dueDateMatch.Groups[1].Value);
                }
                
                // Next, try to find any date in the document
                var dateMatch = Regex.Match(content, @"(?:date|fecha|emisi[oó]n)[:\s]*(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{2,4}|\d{2,4}[-/\.]\d{1,2}[-/\.]\d{1,2})", RegexOptions.IgnoreCase);
                if (dateMatch.Success)
                {
                    // Format the date consistently
                    return StandardizeDate(dateMatch.Groups[1].Value);
                }
                
                // Last resort, look for any date-like pattern
                var anyDateMatch = Regex.Match(content, @"(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{2,4}|\d{2,4}[-/\.]\d{1,2}[-/\.]\d{1,2})", RegexOptions.IgnoreCase);
                if (anyDateMatch.Success)
                {
                    return StandardizeDate(anyDateMatch.Groups[1].Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting date from content");
            }
            
            // Use current date if no date found or if there was an error
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
        
        /// <summary>
        /// Standardizes date format to yyyy-MM-dd
        /// </summary>
        private static string StandardizeDate(string dateString)
        {
            try
            {
                var dateParts = dateString.Split('-', '.', '/');
                if (dateParts.Length == 3)
                {
                    // Handle year-month-day format
                    if (dateParts[0].Length == 4)
                    {
                        return $"{dateParts[0]}-{dateParts[1].PadLeft(2, '0')}-{dateParts[2].PadLeft(2, '0')}";
                    }
                    // Handle day-month-year format
                    else if (dateParts[2].Length == 4 || (dateParts[2].Length == 2 && int.Parse(dateParts[2]) < 50))
                    {
                        string year = dateParts[2].Length == 2 ? $"20{dateParts[2]}" : dateParts[2];
                        return $"{year}-{dateParts[1].PadLeft(2, '0')}-{dateParts[0].PadLeft(2, '0')}";
                    }
                }
            }
            catch
            {
                // If date parsing fails, keep the original format
            }
            
            // Return the original if we couldn't standardize it
            return dateString;
        }



        private static string CleanupFileName(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return string.Empty;
            
            // Replace spaces with underscores first
            string result = filename.Replace(' ', '_');
            
            // Remove common Spanish accents
            result = result.Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u');
            result = result.Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U');
            result = result.Replace('ü', 'u').Replace('Ü', 'U').Replace('ñ', 'n').Replace('Ñ', 'N');
            
            // Replace question marks, commas, semicolons and other problematic characters
            result = result.Replace('?', '_').Replace(',', '_').Replace(';', '_').Replace(':', '_');
            result = result.Replace('\'', '_').Replace('\"', '_').Replace('(', '_').Replace(')', '_');
            
            // Replace invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            result = new string(result.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            
            // Replace multiple underscores with single one
            result = Regex.Replace(result, @"_{2,}", "_");
            
            // Remove leading and trailing underscores
            result = result.Trim('_');
            
            // Truncate to reasonable length (max 50 chars)
            if (result.Length > 50)
            {
                result = result.Substring(0, 50);
            }
            
            // Ensure we don't return an empty string after all this cleaning
            if (string.IsNullOrWhiteSpace(result))
            {
                return "servicio";
            }
            
            return result;
        }
        
        /// <summary>
        /// Extracts the date part from a filename that follows the pattern: service_date_paymentmethod
        /// </summary>
        private static string ExtractDateFromFilename(string filename)
        {
            try
            {
                // Pattern: service_date_paymentmethod, we want the 'date' part
                var parts = filename.Split('_');
                
                if (parts.Length >= 3)
                {
                    // Look for the part that most resembles a date
                    foreach (var part in parts)
                    {
                        // Check for yyyy-MM-dd pattern
                        if (Regex.IsMatch(part, @"^\d{4}-\d{2}-\d{2}$"))
                        {
                            return part;
                        }
                        
                        // Check for dd-MM-yyyy pattern
                        if (Regex.IsMatch(part, @"^\d{2}-\d{2}-\d{4}$"))
                        {
                            // Convert to standard format for parsing
                            var dateParts = part.Split('-');
                            return $"{dateParts[2]}-{dateParts[1]}-{dateParts[0]}";
                        }
                        
                        // Check for yyyy-MM format
                        if (Regex.IsMatch(part, @"^\d{4}-\d{2}$"))
                        {
                            return $"{part}-01"; // Default to first day of month
                        }
                    }
                }
                
                // If no date found in the pattern, try to extract a date from the whole filename
                var dateMatch = Regex.Match(filename, @"(\d{4}-\d{2}-\d{2})");
                if (dateMatch.Success)
                {
                    return dateMatch.Groups[1].Value;
                }
                
                // If still no date, return today's date
                Log.Debug("No date found in filename: {Filename}, using current date", filename);
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting date from filename: {Filename}", filename);
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
        }
        
        /// <summary>
        /// Gets the localized month name for a given month number
        /// </summary>
        private static string GetMonthName(int monthNumber)
        {
            string[] monthNames = 
            [
                "enero", "febrero", "marzo", "abril", "mayo", "junio", 
                "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
            ];
            
            if (monthNumber >= 1 && monthNumber <= 12)
            {
                return monthNames[monthNumber - 1];
            }
            
            return "mes_desconocido";
        }
        
        /// <summary>
        /// Checks if a directory is already part of a year/month folder structure
        /// </summary>
        private static bool IsInYearMonthStructure(string directoryPath)
        {
            try
            {
                // Get the folder name and its parent
                string folderName = new DirectoryInfo(directoryPath).Name;
                string? parentDir = Directory.GetParent(directoryPath)?.FullName;
                
                if (parentDir == null)
                {
                    return false;
                }
                
                string parentFolderName = new DirectoryInfo(parentDir).Name;
                
                // Check if folder name is a month pattern like "01_enero"
                bool isMonthFolder = Regex.IsMatch(folderName, @"^\d{2}_[a-z]+$");
                
                // Check if parent folder name is a year (4 digits)
                bool isYearFolder = Regex.IsMatch(parentFolderName, @"^\d{4}$");
                
                // It's in a year/month structure if both conditions are met
                return isMonthFolder && isYearFolder;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking folder structure: {DirectoryPath}", directoryPath);
                return false;
            }
        }
    }
}
