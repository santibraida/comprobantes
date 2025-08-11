using Serilog;

namespace FileContentRenamer.Services
{
    public class TextProcessor : IFileProcessor
    {
        public bool CanProcess(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".txt", StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<string> ExtractContentAsync(string filePath)
        {
            try
            {
                // For text files, simply read the content
                using StreamReader reader = new StreamReader(filePath);
                // Read up to 1000 characters or the entire file, whichever is smaller
                char[] buffer = new char[1000];
                int charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                return new string(buffer, 0, charsRead);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reading text file");
                return string.Empty;
            }
        }
    }
}
