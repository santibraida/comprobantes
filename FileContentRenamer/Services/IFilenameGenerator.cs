namespace FileContentRenamer.Services
{
    public interface IFilenameGenerator
    {
        /// <summary>
        /// Generates a filename based on content and original path
        /// </summary>
        /// <param name="content">File content for date extraction</param>
        /// <param name="originalPath">Original file path</param>
        /// <returns>Generated filename</returns>
        string GenerateFilename(string content, string originalPath);

        /// <summary>
        /// Checks if a filename follows the naming convention
        /// </summary>
        /// <param name="filenameWithoutExtension">The filename to check (without extension)</param>
        /// <returns>True if the filename follows the naming convention</returns>
        bool IsAlreadyNamedFilename(string filenameWithoutExtension);

        /// <summary>
        /// Generates a unique filename by appending a counter if the file already exists
        /// </summary>
        /// <param name="directory">Directory where the file will be placed</param>
        /// <param name="baseFilename">Base filename</param>
        /// <returns>Unique filename</returns>
        string GenerateUniqueFilename(string directory, string baseFilename);
    }
}
