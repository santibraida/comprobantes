namespace FileContentRenamer.Services
{
    public interface IDateExtractor
    {
        /// <summary>
        /// Extracts date from file content using various patterns and priorities
        /// </summary>
        /// <param name="content">The content to extract date from</param>
        /// <returns>Standardized date string in yyyy-MM-dd format, or empty string if no date found</returns>
        string ExtractDateFromContent(string content);

        /// <summary>
        /// Extracts date from filename that follows the naming convention
        /// </summary>
        /// <param name="filename">The filename to extract date from</param>
        /// <returns>Date string in yyyy-MM-dd format, or empty string if no date found</returns>
        string ExtractDateFromFilename(string filename);
    }
}
