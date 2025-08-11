namespace FileContentRenamer.Services
{
    public interface IFileProcessor
    {
        bool CanProcess(string filePath);
        Task<string> ExtractContentAsync(string filePath);
    }
}
