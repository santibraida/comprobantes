using FileContentRenamer.Models;

namespace FileContentRenamer.Tests.Helpers
{
  /// <summary>
  /// Mock file provider for testing AppConfig without touching real file system
  /// </summary>
  public class MockConfigurationFileProvider : IConfigurationFileProvider
  {
    private readonly Dictionary<string, string> _files = new();
    private readonly HashSet<string> _directories = new();
    private readonly string _baseDirectory;

    public MockConfigurationFileProvider(string baseDirectory = "/mock")
    {
      _baseDirectory = baseDirectory;
      _directories.Add(baseDirectory);
    }

    public void AddFile(string path, string content)
    {
      _files[path] = content;
      // Auto-create parent directories
      var dir = Path.GetDirectoryName(path);
      while (!string.IsNullOrEmpty(dir))
      {
        _directories.Add(dir);
        dir = Path.GetDirectoryName(dir);
      }
    }

    public void AddDirectory(string path)
    {
      _directories.Add(path);
    }

    public bool FileExists(string path) => _files.ContainsKey(path);

    public bool DirectoryExists(string path) => _directories.Contains(path);

    public string ReadAllText(string path)
    {
      if (_files.TryGetValue(path, out var content))
        return content;
      throw new FileNotFoundException($"File not found: {path}");
    }

    public void WriteAllBytes(string path, byte[] bytes)
    {
      _files[path] = System.Text.Encoding.UTF8.GetString(bytes);
    }

    public string GetBaseDirectory() => _baseDirectory;

    public string? GetParentDirectory(string path) => Path.GetDirectoryName(path);

    public string GetFullPath(string path)
    {
      if (Path.IsPathRooted(path))
        return path;
      return Path.Combine(_baseDirectory, path);
    }

    public string? GetFileContent(string path) => _files.TryGetValue(path, out var content) ? content : null;
  }
}
