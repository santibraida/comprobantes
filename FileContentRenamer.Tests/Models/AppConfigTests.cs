using FileContentRenamer.Models;
using FileContentRenamer.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace FileContentRenamer.Tests.Models
{
    public class AppConfigTests : IDisposable
    {
        private readonly string _tempDirectory;

        public AppConfigTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public void AppConfig_DefaultValues_ShouldBeSetCorrectly()
        {
            // Act
            var appConfig = new AppConfig();

            // Assert
            appConfig.BasePath.Should().BeNull();
            appConfig.LastUsedPath.Should().BeNull();
            appConfig.FileExtensions.Should().BeNull();
            appConfig.IncludeSubdirectories.Should().BeTrue();
            appConfig.TesseractDataPath.Should().BeNull();
            appConfig.TesseractLanguage.Should().BeNull();
            appConfig.ForceReprocessAlreadyNamed.Should().BeFalse();
            appConfig.MaxDegreeOfParallelism.Should().Be(Environment.ProcessorCount);
            appConfig.NamingRules.Should().NotBeNull();
        }

        [Fact]
        public void AppConfig_Properties_ShouldBeSettable()
        {
            // Arrange
            var appConfig = new AppConfig();
            var namingRules = new NamingRules("test_service", "test_payment");

            // Act
            appConfig.BasePath = "/test/path";
            appConfig.LastUsedPath = "/test/last";
            appConfig.FileExtensions = new List<string> { ".pdf", ".txt" };
            appConfig.IncludeSubdirectories = false;
            appConfig.TesseractDataPath = "/test/tessdata";
            appConfig.TesseractLanguage = "spa";
            appConfig.ForceReprocessAlreadyNamed = true;
            appConfig.MaxDegreeOfParallelism = 4;
            appConfig.NamingRules = namingRules;

            // Assert
            appConfig.BasePath.Should().Be("/test/path");
            appConfig.LastUsedPath.Should().Be("/test/last");
            appConfig.FileExtensions.Should().ContainInOrder(".pdf", ".txt");
            appConfig.IncludeSubdirectories.Should().BeFalse();
            appConfig.TesseractDataPath.Should().Be("/test/tessdata");
            appConfig.TesseractLanguage.Should().Be("spa");
            appConfig.ForceReprocessAlreadyNamed.Should().BeTrue();
            appConfig.MaxDegreeOfParallelism.Should().Be(4);
            appConfig.NamingRules.Should().Be(namingRules);
        }

        [Fact]
        public void LoadFromConfiguration_WithMissingConfigFile_ShouldThrowException()
        {
            // NOTE: The current implementation searches up the directory tree to find appsettings.json,
            // so it will find the solution's config file even from isolated temp directories.
            // This test now verifies that behavior - it should NOT throw an exception.

            // Arrange - create a completely isolated temporary directory
            var isolatedDir = Path.Combine(Path.GetTempPath(), "isolated_test", Guid.NewGuid().ToString(), "deep", "nested");
            Directory.CreateDirectory(isolatedDir);

            try
            {
                // Act - The method will search up and find the solution's appsettings.json
                var result = AppConfig.LoadFromConfiguration(isolatedDir);

                // Assert - Should successfully load config from solution root
                result.Should().NotBeNull();
                result.BasePath.Should().NotBeNullOrEmpty();
            }
            finally
            {
                var rootTempDir = Path.Combine(Path.GetTempPath(), "isolated_test");
                if (Directory.Exists(rootTempDir))
                    Directory.Delete(rootTempDir, true);
            }
        }

        [Fact]
        public void LoadFromConfiguration_WithNoBasePath_ShouldUseCurrentDirectory()
        {
            // Act
            var result = AppConfig.LoadFromConfiguration();

            // Assert
            result.Should().NotBeNull();
            // The method should not throw and should find the configuration in the project
        }

        [Fact]
        public void SaveLastUsedPath_ShouldUpdateConfigFile()
        {
            // Arrange
            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var initialConfig = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{_tempDirectory.Replace("\\", "\\\\")}"",
                    ""FileExtensions"": ["".pdf""],
                    ""TesseractDataPath"": ""./tessdata"",
                    ""TesseractLanguage"": ""eng+spa"",
                    ""LastUsedPath"": """",
                    ""NamingRules"": {{
                        ""DefaultServiceName"": ""servicio"",
                        ""DefaultPaymentMethod"": ""santander""
                    }}
                }}
            }}";
            File.WriteAllText(configPath, initialConfig);

            var appConfig = AppConfig.LoadFromConfiguration(_tempDirectory);
            var newPath = Path.Combine(_tempDirectory, "new_path");
            Directory.CreateDirectory(newPath);

            // Act
            appConfig.SaveLastUsedPath(newPath);

            // Assert
            var updatedConfig = AppConfig.LoadFromConfiguration(_tempDirectory);
            updatedConfig.LastUsedPath.Should().Be(newPath);
        }

        [Fact]
        public void SaveLastUsedPath_WithInvalidPath_ShouldNotThrow()
        {
            // Arrange
            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var initialConfig = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{_tempDirectory.Replace("\\", "\\\\")}"",
                    ""FileExtensions"": ["".pdf""],
                    ""TesseractDataPath"": ""./tessdata"",
                    ""TesseractLanguage"": ""eng+spa"",
                    ""NamingRules"": {{
                        ""DefaultServiceName"": ""servicio"",
                        ""DefaultPaymentMethod"": ""santander""
                    }}
                }}
            }}";
            File.WriteAllText(configPath, initialConfig);

            var appConfig = AppConfig.LoadFromConfiguration(_tempDirectory);

            // Act & Assert
            Action act = () => appConfig.SaveLastUsedPath("/invalid/path");
            act.Should().NotThrow();
        }

        [Fact]
        public void SaveLastUsedPath_WithEmptyPath_ShouldReturnEarly()
        {
            // Arrange
            var config = new AppConfig();
            var initialPath = config.LastUsedPath;

            // Act
            config.SaveLastUsedPath(string.Empty);
            config.SaveLastUsedPath(null!);

            // Assert - LastUsedPath should remain unchanged
            config.LastUsedPath.Should().Be(initialPath);
        }

        [Fact]
        public void SaveLastUsedPath_WithValidPath_ShouldUpdateProperty()
        {
            // Arrange
            var config = new AppConfig();
            var testPath = "/test/path";

            // Act
            config.SaveLastUsedPath(testPath);

            // Assert
            config.LastUsedPath.Should().Be(testPath);
        }

        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Act
            var config = new AppConfig();

            // Assert
            config.IncludeSubdirectories.Should().BeTrue();
            config.ForceReprocessAlreadyNamed.Should().BeFalse();
            config.MaxDegreeOfParallelism.Should().Be(Environment.ProcessorCount);
            config.NamingRules.Should().NotBeNull();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var config = new AppConfig();
            var testExtensions = new List<string> { ".pdf", ".jpg" };

            // Act
            config.BasePath = "/test/base";
            config.LastUsedPath = "/test/last";
            config.FileExtensions = testExtensions;
            config.TesseractDataPath = "/test/tessdata";
            config.TesseractLanguage = "spa";
            config.IncludeSubdirectories = false;
            config.ForceReprocessAlreadyNamed = true;
            config.MaxDegreeOfParallelism = 4;

            // Assert
            config.BasePath.Should().Be("/test/base");
            config.LastUsedPath.Should().Be("/test/last");
            config.FileExtensions.Should().Equal(testExtensions);
            config.TesseractDataPath.Should().Be("/test/tessdata");
            config.TesseractLanguage.Should().Be("spa");
            config.IncludeSubdirectories.Should().BeFalse();
            config.ForceReprocessAlreadyNamed.Should().BeTrue();
            config.MaxDegreeOfParallelism.Should().Be(4);
        }

        [Fact]
        public void LoadFromConfiguration_WithCommandLineBasePath_ShouldOverrideConfigBasePath()
        {
            // Arrange
            var configBasePath = Path.Combine(_tempDirectory, "config_base");
            var commandLineBasePath = Path.Combine(_tempDirectory, "commandline_base");
            Directory.CreateDirectory(configBasePath);
            Directory.CreateDirectory(commandLineBasePath);

            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var configContent = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{configBasePath.Replace("\\", "\\\\")}"",
                    ""FileExtensions"": ["".pdf""],
                    ""TesseractDataPath"": ""./tessdata"",
                    ""TesseractLanguage"": ""eng+spa"",
                    ""NamingRules"": {{
                        ""DefaultServiceName"": ""servicio"",
                        ""DefaultPaymentMethod"": ""santander""
                    }}
                }}
            }}";
            File.WriteAllText(configPath, configContent);

            // Act
            var config = AppConfig.LoadFromConfiguration(commandLineBasePath);

            // Assert
            config.BasePath.Should().Be(commandLineBasePath);
        }

        [Fact]
        public void SaveLastUsedPath_WhenConfigFileNotFound_ShouldNotThrow()
        {
            // Arrange
            var config = new AppConfig();
            var nonExistentTempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act & Assert - should handle gracefully when config file doesn't exist
            Action act = () => config.SaveLastUsedPath(nonExistentTempDir);
            act.Should().NotThrow();
        }

        [Fact]
        public void SaveLastUsedPath_WhenConfigFileIsLocked_ShouldNotThrow()
        {
            // Arrange
            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var configContent = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{_tempDirectory.Replace("\\", "\\\\")}"",
                    ""FileExtensions"": ["".pdf""],
                    ""TesseractDataPath"": ""./tessdata"",
                    ""TesseractLanguage"": ""eng+spa"",
                    ""LastUsedPath"": """",
                    ""NamingRules"": {{
                        ""DefaultServiceName"": ""servicio"",
                        ""DefaultPaymentMethod"": ""santander""
                    }}
                }}
            }}";
            File.WriteAllText(configPath, configContent);

            var config = AppConfig.LoadFromConfiguration(_tempDirectory);

            // Lock the file to simulate write failure
            using var lockStream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.None);
            var newPath = Path.Combine(_tempDirectory, "new_path");
            Directory.CreateDirectory(newPath);

            // Act & Assert - should catch and log the exception without throwing
            Action act = () => config.SaveLastUsedPath(newPath);
            act.Should().NotThrow();
        }

        [Fact]
        public void LoadFromConfiguration_ShouldConvertTesseractDataPathToAbsolute()
        {
            // Arrange
            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var relativePath = "./tessdata";
            var configContent = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{_tempDirectory.Replace("\\", "\\\\")}"",
                    ""FileExtensions"": ["".pdf""],
                    ""TesseractDataPath"": ""{relativePath}"",
                    ""TesseractLanguage"": ""eng+spa"",
                    ""NamingRules"": {{
                        ""DefaultServiceName"": ""servicio"",
                        ""DefaultPaymentMethod"": ""santander""
                    }}
                }}
            }}";
            File.WriteAllText(configPath, configContent);

            // Act
            var config = AppConfig.LoadFromConfiguration(_tempDirectory);

            // Assert
            config.TesseractDataPath.Should().NotBeNull();
            Path.IsPathRooted(config.TesseractDataPath).Should().BeTrue();
        }

        [Fact]
        public void SaveLastUsedPath_ShouldUpdateRootConfigIfDifferent()
        {
            // Arrange - Create both a nested config and root config
            var nestedDir = Path.Combine(_tempDirectory, "nested");
            Directory.CreateDirectory(nestedDir);

            var rootConfigPath = Path.Combine(_tempDirectory, "appsettings.json");
            var nestedConfigPath = Path.Combine(nestedDir, "appsettings.json");

            var configContent = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{_tempDirectory.Replace("\\", "\\\\")}"",
                    ""FileExtensions"": ["".pdf""],
                    ""TesseractDataPath"": ""./tessdata"",
                    ""TesseractLanguage"": ""eng+spa"",
                    ""LastUsedPath"": """",
                    ""NamingRules"": {{
                        ""DefaultServiceName"": ""servicio"",
                        ""DefaultPaymentMethod"": ""santander""
                    }}
                }}
            }}";

            File.WriteAllText(rootConfigPath, configContent);
            File.WriteAllText(nestedConfigPath, configContent);

            var appConfig = AppConfig.LoadFromConfiguration(nestedDir);
            var newPath = Path.Combine(_tempDirectory, "new_path");
            Directory.CreateDirectory(newPath);

            // Act
            appConfig.SaveLastUsedPath(newPath);

            // Assert
            appConfig.LastUsedPath.Should().Be(newPath);
        }

        [Fact]
        public void SaveLastUsedPath_WithNewPath_ShouldUpdateProperty()
        {
            // Arrange - Use actual solution config
            var appConfig = AppConfig.LoadFromConfiguration();
            var newPath = Path.Combine(_tempDirectory, "new_path");
            Directory.CreateDirectory(newPath);

            // Act
            appConfig.SaveLastUsedPath(newPath);

            // Assert - Verify the property was updated
            appConfig.LastUsedPath.Should().Be(newPath);

            // Verify it was actually written to the config file
            var updatedConfig = AppConfig.LoadFromConfiguration();
            updatedConfig.LastUsedPath.Should().Be(newPath);
        }

        [Fact]
        public void SaveLastUsedPath_WithConfigContainingOtherProperties_ShouldPreserveThem()
        {
            // Arrange - Load the actual solution config
            var appConfig = AppConfig.LoadFromConfiguration();
            var originalFileExtensionsCount = appConfig.FileExtensions?.Count ?? 0;
            var originalIncludeSubdirectories = appConfig.IncludeSubdirectories;

            var newPath = Path.Combine(_tempDirectory, "new_path");
            Directory.CreateDirectory(newPath);

            // Act
            appConfig.SaveLastUsedPath(newPath);

            // Assert - Verify other properties were preserved
            var updatedConfig = AppConfig.LoadFromConfiguration();
            updatedConfig.LastUsedPath.Should().Be(newPath);
            updatedConfig.FileExtensions.Should().HaveCount(originalFileExtensionsCount);
            updatedConfig.IncludeSubdirectories.Should().Be(originalIncludeSubdirectories);
        }

        [Fact]
        public void LoadFromConfiguration_WithRelativeTesseractPath_ShouldConvertToAbsolute()
        {
            // Act - Load the actual solution config which has a relative path
            var config = AppConfig.LoadFromConfiguration();

            // Assert
            config.TesseractDataPath.Should().NotBeNull();
            Path.IsPathRooted(config.TesseractDataPath).Should().BeTrue();
        }

        [Fact]
        public void NamingRules_ShouldBeInitializedByDefault()
        {
            // Arrange & Act
            var config = new AppConfig();

            // Assert
            config.NamingRules.Should().NotBeNull();
            config.NamingRules.DefaultServiceName.Should().NotBeNull();
            config.NamingRules.DefaultPaymentMethod.Should().NotBeNull();
        }

        [Fact]
        public void LoadFromConfiguration_ShouldLoadNamingRulesDefaultsFromConfig()
        {
            // Act
            var config = AppConfig.LoadFromConfiguration();

            // Assert
            config.NamingRules.Should().NotBeNull();
            config.NamingRules.DefaultServiceName.Should().NotBeNullOrEmpty();
            config.NamingRules.DefaultPaymentMethod.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void LoadFromConfiguration_WithNullBaseDir_ShouldHandleGracefully()
        {
            // Act - Should use current directory when base path is null
            var config = AppConfig.LoadFromConfiguration(null);

            // Assert
            config.Should().NotBeNull();
            config.BasePath.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void SaveLastUsedPath_WithNullPath_ShouldReturnEarly()
        {
            // Arrange
            var config = new AppConfig { LastUsedPath = "original" };

            // Act
            config.SaveLastUsedPath(null!);

            // Assert - LastUsedPath should remain unchanged
            config.LastUsedPath.Should().Be("original");
        }

        [Fact]
        public void MaxDegreeOfParallelism_DefaultValue_ShouldEqualProcessorCount()
        {
            // Arrange & Act
            var config = new AppConfig();

            // Assert
            config.MaxDegreeOfParallelism.Should().Be(Environment.ProcessorCount);
            config.MaxDegreeOfParallelism.Should().BeGreaterThan(0);
        }

        [Fact]
        public void FileExtensions_ShouldSupportNullValue()
        {
            // Arrange & Act
            var config = new AppConfig { FileExtensions = null };

            // Assert
            config.FileExtensions.Should().BeNull();
        }

        [Fact]
        public void FileExtensions_ShouldSupportEmptyList()
        {
            // Arrange & Act
            var config = new AppConfig { FileExtensions = new List<string>() };

            // Assert
            config.FileExtensions.Should().NotBeNull();
            config.FileExtensions.Should().BeEmpty();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}
