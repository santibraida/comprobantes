namespace FileContentRenamer.Services
{
    /// <summary>
    /// Interface for file processing services that scan and rename files
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Processes files in the configured directory and renames them based on content
        /// </summary>
        Task ProcessFilesAsync();
    }
}
