namespace FileContentRenamer.Models
{
  /// <summary>
  /// Interface for file system operations used by AppConfig
  /// </summary>
  public interface IConfigurationFileProvider
  {
    bool FileExists(string path);
    bool DirectoryExists(string path);
    string ReadAllText(string path);
    void WriteAllBytes(string path, byte[] bytes);
    string GetBaseDirectory();
    string? GetParentDirectory(string path);
    string GetFullPath(string path);
  }
}
