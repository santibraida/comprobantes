namespace FileContentRenamer.Models
{
  /// <summary>
  /// Interface for application configuration
  /// </summary>
  public interface IAppConfig
  {
    /// <summary>
    /// Base path to scan for files
    /// </summary>
    string? BasePath { get; set; }

    /// <summary>
    /// Last used path by the user
    /// </summary>
    string? LastUsedPath { get; set; }

    /// <summary>
    /// File extensions to process
    /// </summary>
    List<string>? FileExtensions { get; set; }

    /// <summary>
    /// Whether to include subdirectories when scanning
    /// </summary>
    bool IncludeSubdirectories { get; set; }

    /// <summary>
    /// Path to Tesseract OCR data files
    /// </summary>
    string? TesseractDataPath { get; set; }

    /// <summary>
    /// Tesseract OCR language(s) to use
    /// </summary>
    string? TesseractLanguage { get; set; }

    /// <summary>
    /// Whether to force reprocessing of already named files
    /// </summary>
    bool ForceReprocessAlreadyNamed { get; set; }

    /// <summary>
    /// Maximum degree of parallelism for file processing
    /// </summary>
    int MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// Naming rules for generating filenames
    /// </summary>
    NamingRules NamingRules { get; set; }

    /// <summary>
    /// Saves the LastUsedPath configuration to appsettings.json
    /// </summary>
    void SaveLastUsedPath(string path);
  }
}
