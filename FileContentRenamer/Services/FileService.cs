using FileContentRenamer.Models;
using Serilog;
using System.Diagnostics;

namespace FileContentRenamer.Services
{
    public class FileService : IFileService
    {
        private readonly AppConfig _config;
        private readonly List<IFileProcessor> _processors;
        private readonly IDateExtractor _dateExtractor;
        private readonly IDirectoryOrganizer _directoryOrganizer;
        private readonly IFilenameGenerator _filenameGenerator;
        private readonly IFileValidator _fileValidator;

        public FileService(
            AppConfig config,
            List<IFileProcessor> processors,
            IDateExtractor dateExtractor,
            IDirectoryOrganizer directoryOrganizer,
            IFilenameGenerator filenameGenerator,
            IFileValidator fileValidator)
        {
            _config = config;
            _processors = processors;
            _dateExtractor = dateExtractor;
            _directoryOrganizer = directoryOrganizer;
            _filenameGenerator = filenameGenerator;
            _fileValidator = fileValidator;
        }

        public async Task ProcessFilesAsync()
        {
            if (!_fileValidator.ValidateConfiguration())
            {
                return;
            }

            // Start timing the file processing
            var stopwatch = Stopwatch.StartNew();

            // Get all files with the specified extensions
            var searchOption = _config.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var files = _config.FileExtensions!
                .SelectMany(ext => Directory.GetFiles(_config.BasePath!, $"*{ext}", searchOption))
                .Where(_fileValidator.ShouldProcessFile)
                .ToList();

            Log.Information("Starting to process {FileCount} files in {BasePath} with parallelism degree: {MaxDegreeOfParallelism}", files.Count, _config.BasePath, _config.MaxDegreeOfParallelism);

            int processedCount = 0;
            var processedCountLock = new object();

            // Configure parallel processing options
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism
            };

            Log.Debug("Processing files with parallelism degree: {MaxDegreeOfParallelism}", _config.MaxDegreeOfParallelism);

            await Parallel.ForEachAsync(files, parallelOptions, async (filePath, cancellationToken) =>
            {
                await ProcessFileAsync(filePath);

                lock (processedCountLock)
                {
                    processedCount++;
                    if (processedCount % 10 == 0)
                    {
                        Log.Debug("Processed {ProcessedCount}/{TotalCount} files", processedCount, files.Count);
                    }
                }
            });

            stopwatch.Stop();
            Log.Information("Finished processing {FileCount} files in {ElapsedTime}ms ({ElapsedSeconds:F2} seconds)",
                files.Count, stopwatch.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds / 1000.0);
        }

        private async Task ProcessFileAsync(string filePath)
        {
            var fileStopwatch = Stopwatch.StartNew();
            try
            {
                var fileName = Path.GetFileName(filePath);
                Log.Information("Processing file: {FileName}", fileName);

                _fileValidator.ValidateFileSize(filePath);

                string? directory = Path.GetDirectoryName(filePath);
                if (directory == null)
                {
                    Log.Error("Could not determine directory for {FilePath}", filePath);
                    return;
                }

                string baseName = Path.GetFileNameWithoutExtension(fileName);

                // Check if file should be skipped due to already being named
                if (_fileValidator.ShouldSkipAlreadyNamedFile(filePath))
                {
                    Log.Information("File already follows naming convention, organizing into folder structure: {FileName}", fileName);

                    // Extract date and organize into year/month folder
                    string dateStr = _dateExtractor.ExtractDateFromFilename(baseName);
                    string newPath = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(filePath, dateStr);

                    if (newPath != filePath)
                    {
                        Log.Information("Moved already-named file to organized structure: {FileName}", fileName);
                    }
                    return;
                }

                // Find a processor for this file type
                var processor = _processors.FirstOrDefault(p => p.CanProcess(filePath));
                if (processor == null)
                {
                    Log.Warning("No processor available for file type: {FileName}", fileName);
                    return;
                }

                // Extract content from the file
                string content = await processor.ExtractContentAsync(filePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    Log.Warning("No content extracted from file: {FileName}", fileName);
                    return;
                }

                // Generate new filename
                string newFilename = _filenameGenerator.GenerateFilename(content, filePath);

                // Check if rename is needed
                if (string.Equals(fileName, newFilename, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("File already has an appropriate name: {FileName}", fileName);

                    // Still organize into year/month folder structure
                    string dateStr = _dateExtractor.ExtractDateFromContent(content);
                    _directoryOrganizer.OrganizeFileIntoDirectoryStructure(filePath, dateStr);
                    return;
                }

                // Generate unique filename if needed
                string targetPath = _filenameGenerator.GenerateUniqueFilename(directory, newFilename);

                // Rename the file
                File.Move(filePath, targetPath);
                Log.Information("Renaming: {OldName} -> {NewName}", fileName, Path.GetFileName(targetPath));

                // Organize renamed file into directory structure
                string finalDateStr = _dateExtractor.ExtractDateFromContent(content);
                _directoryOrganizer.OrganizeFileIntoDirectoryStructure(targetPath, finalDateStr);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing file: {FileName}", Path.GetFileName(filePath));
            }
            finally
            {
                fileStopwatch.Stop();
                Log.Debug("Completed processing file: {FileName} in {ElapsedTime}ms",
                    Path.GetFileName(filePath), fileStopwatch.ElapsedMilliseconds);
            }
        }
    }
}
