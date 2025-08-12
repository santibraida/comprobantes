namespace FileContentRenamer.Services
{
    public interface IFileValidator
    {
        /// <summary>
        /// Determines if a file should be processed based on various criteria
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if the file should be processed</returns>
        bool ShouldProcessFile(string filePath);

        /// <summary>
        /// Determines if an already named file should be skipped
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>True if the file should be skipped</returns>
        bool ShouldSkipAlreadyNamedFile(string filePath);

        /// <summary>
        /// Validates the application configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        bool ValidateConfiguration();

        /// <summary>
        /// Validates file size against configured limits (throws exception if invalid)
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        void ValidateFileSize(string filePath);
    }
}
