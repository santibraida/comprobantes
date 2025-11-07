using FileContentRenamer.Models;
using FluentAssertions;
using Xunit;

namespace FileContentRenamer.Tests
{
  /// <summary>
  /// Integration tests for the main Program entry point
  /// These tests verify the initialization and setup logic of the application
  /// </summary>
  public class ProgramIntegrationTests
  {
    [Fact]
    public void AppConfig_LoadsSuccessfully()
    {
      // Arrange & Act
      var config = AppConfig.LoadFromConfiguration();

      // Assert
      config.Should().NotBeNull();
      config.BasePath.Should().NotBeNullOrEmpty();
      config.TesseractDataPath.Should().NotBeNullOrEmpty();
      config.TesseractLanguage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AppConfig_HasValidDefaults()
    {
      // Arrange & Act
      var config = AppConfig.LoadFromConfiguration();

      // Assert - verify default values are set
      config.IncludeSubdirectories.Should().BeTrue();
      // ForceReprocessAlreadyNamed can be either true or false based on config
      config.MaxDegreeOfParallelism.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AppConfig_NamingRulesAreInitialized()
    {
      // Arrange & Act
      var config = AppConfig.LoadFromConfiguration();

      // Assert
      config.NamingRules.Should().NotBeNull();
      config.NamingRules.DefaultServiceName.Should().NotBeNullOrEmpty();
      config.NamingRules.DefaultPaymentMethod.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AppConfig_FileExtensionsAreConfigured()
    {
      // Arrange & Act
      var config = AppConfig.LoadFromConfiguration();

      // Assert
      config.FileExtensions.Should().NotBeNullOrEmpty();
      config.FileExtensions.Should().Contain(ext => ext.ToLowerInvariant().Contains("pdf")
                                                  || ext.ToLowerInvariant().Contains("txt")
                                                  || ext.ToLowerInvariant().Contains("jpg"));
    }

    [Fact]
    public void AppConfig_TesseractPathIsAccessible()
    {
      // Arrange & Act
      var config = AppConfig.LoadFromConfiguration();

      // Assert
      if (!string.IsNullOrEmpty(config.TesseractDataPath))
      {
        // The path should exist or be a valid path format
        config.TesseractDataPath.Should().NotBeEmpty();
        // Just verify it looks like a reasonable path
        config.TesseractDataPath.Should().Contain("tessdata", "Tesseract data path should contain 'tessdata'");
      }
    }

    [Fact]
    public void AppConfig_SaveLastUsedPath_DoesNotThrow()
    {
      // Arrange
      var config = AppConfig.LoadFromConfiguration();
      string testPath = Path.Combine(Path.GetTempPath(), "test_path");

      // Act & Assert
      // Just verify the method doesn't throw an exception
      var act = () => config.SaveLastUsedPath(testPath);
      act.Should().NotThrow();
    }

    [Fact]
    public void AppConfig_TesseractLanguageIsConfigured()
    {
      // Arrange & Act
      var config = AppConfig.LoadFromConfiguration();

      // Assert
      // The language should be one of the standard values or a valid Tesseract language code
      config.TesseractLanguage.Should().NotBeNullOrEmpty();
      // Verify it's not an invalid value
      if (!string.IsNullOrEmpty(config.TesseractLanguage))
      {
        config.TesseractLanguage.Length.Should().BeLessThanOrEqualTo(20, "Language code should be reasonable length");
      }
    }

    [Fact]
    public void AppConfig_MaxDegreeOfParallelismIsReasonable()
    {
      // Arrange & Act
      var config = AppConfig.LoadFromConfiguration();

      // Assert
      config.MaxDegreeOfParallelism.Should().BeGreaterThanOrEqualTo(1, "Parallelism should be at least 1");
      config.MaxDegreeOfParallelism.Should().BeLessThanOrEqualTo(Environment.ProcessorCount * 2,
          "Parallelism shouldn't exceed processor count by too much");
    }

    [Fact]
    public void AppConfig_ConfigurationFileIsFound()
    {
      // This test verifies that the configuration file resolution works

      // Act
      var config = AppConfig.LoadFromConfiguration();

      // Assert
      config.Should().NotBeNull("Configuration should load successfully");
    }

    [Fact]
    public void AppConfig_HandlesNonExistentPath()
    {
      // Arrange - Use an invalid path that doesn't exist
      string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

      // Act
      // When an invalid path is provided, the app still loads config from the default location
      // and may or may not throw depending on whether default config exists
      var config = AppConfig.LoadFromConfiguration(invalidPath);

      // Assert
      // The config should still load, but the path won't be set to the invalid one
      config.Should().NotBeNull();
    }
  }
}
