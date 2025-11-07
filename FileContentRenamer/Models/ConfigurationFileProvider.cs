namespace FileContentRenamer.Models
{
  /// <summary>
  /// Default implementation of IConfigurationFileProvider using real file system
  /// </summary>
  public class ConfigurationFileProvider : IConfigurationFileProvider
  {
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

    public string GetBaseDirectory() => AppDomain.CurrentDomain.BaseDirectory ?? ".";

    public string? GetParentDirectory(string path) => Directory.GetParent(path)?.FullName;

    public string GetFullPath(string path) => Path.GetFullPath(path);
  }
}
