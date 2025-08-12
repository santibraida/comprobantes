namespace FileContentRenamer.Services
{
    public interface IDirectoryOrganizer
    {
        /// <summary>
        /// Organizes a file into year/month directory structure based on the extracted date
        /// </summary>
        /// <param name="filePath">Current file path</param>
        /// <param name="dateStr">Date in yyyy-MM-dd format</param>
        /// <returns>The new file path after organization</returns>
        string OrganizeFileIntoDirectoryStructure(string filePath, string dateStr);

        /// <summary>
        /// Determines if a directory path follows the year/month directory structure
        /// </summary>
        /// <param name="directoryPath">The directory path to check</param>
        /// <returns>True if the directory follows the year/month structure</returns>
        bool IsInYearMonthStructure(string directoryPath);

        /// <summary>
        /// Gets the month name in Spanish
        /// </summary>
        /// <param name="monthNumber">Month number (1-12)</param>
        /// <returns>Spanish month name</returns>
        string GetMonthName(int monthNumber);
    }
}
