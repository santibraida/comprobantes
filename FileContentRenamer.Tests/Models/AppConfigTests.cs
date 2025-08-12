using FileContentRenamer.Models;
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
        public void LoadFromConfiguration_WithValidConfigFile_ShouldLoadCorrectly()
        {
            // Arrange
            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var configContent = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{_tempDirectory.Replace("\\", "\\\\")}"",
                    ""LastUsedPath"": ""{_tempDirectory.Replace("\\", "\\\\")}"",
                    ""FileExtensions"": ["".pdf"", "".txt""],
                    ""IncludeSubdirectories"": false,
                    ""TesseractDataPath"": ""/test/tessdata"",
                    ""TesseractLanguage"": ""spa"",
                    ""ForceReprocessAlreadyNamed"": true,
                    ""MaxDegreeOfParallelism"": 2,
                    ""NamingRules"": {{
                        ""DefaultServiceName"": ""test_service"",
                        ""DefaultPaymentMethod"": ""test_payment"",
                        ""Rules"": []
                    }}
                }}
            }}";
            File.WriteAllText(configPath, configContent);

            // Act
            var result = AppConfig.LoadFromConfiguration(_tempDirectory);

            // Assert
            result.BasePath.Should().Be(_tempDirectory);
            result.LastUsedPath.Should().Be(_tempDirectory);
            result.FileExtensions.Should().ContainInOrder(".pdf", ".txt");
            result.IncludeSubdirectories.Should().BeFalse();
            result.TesseractDataPath.Should().Be("/test/tessdata");
            result.TesseractLanguage.Should().Be("spa");
            result.ForceReprocessAlreadyNamed.Should().BeTrue();
            result.MaxDegreeOfParallelism.Should().Be(2);
            result.NamingRules.DefaultServiceName.Should().Be("test_service");
            result.NamingRules.DefaultPaymentMethod.Should().Be("test_payment");
        }

        [Fact]
        public void LoadFromConfiguration_WithMissingConfigFile_ShouldThrowException()
        {
            // Arrange - create a completely isolated temporary directory far from any solution
            var isolatedDir = Path.Combine(Path.GetTempPath(), "isolated_test", Guid.NewGuid().ToString(), "deep", "nested");
            Directory.CreateDirectory(isolatedDir);

            try
            {
                // Act & Assert - Should throw FileNotFoundException when appsettings.json is not found
                Action act = () => AppConfig.LoadFromConfiguration(isolatedDir);
                act.Should().Throw<FileNotFoundException>()
                   .WithMessage("*appsettings.json not found*");
            }
            finally
            {
                var rootTempDir = Path.Combine(Path.GetTempPath(), "isolated_test");
                if (Directory.Exists(rootTempDir))
                    Directory.Delete(rootTempDir, true);
            }
        }

        [Fact]
        public void LoadFromConfiguration_WithInvalidJson_ShouldThrowException()
        {
            // Arrange
            var isolatedDir = Path.Combine(Path.GetTempPath(), "invalid_json_test", Guid.NewGuid().ToString());
            Directory.CreateDirectory(isolatedDir);
            
            var configPath = Path.Combine(isolatedDir, "appsettings.json");
            File.WriteAllText(configPath, "{ invalid json content");

            try
            {
                // Act & Assert - Should throw exception when JSON is malformed
                Action act = () => AppConfig.LoadFromConfiguration(isolatedDir);
                act.Should().Throw<Exception>();
            }
            finally
            {
                var rootTempDir = Path.Combine(Path.GetTempPath(), "invalid_json_test");
                if (Directory.Exists(rootTempDir))
                    Directory.Delete(rootTempDir, true);
            }
        }

        [Fact]
        public void LoadFromConfiguration_WithEmptyConfig_ShouldThrowException()
        {
            // Arrange
            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var emptyConfig = @"{ ""AppConfig"": {} }";
            File.WriteAllText(configPath, emptyConfig);

            // Act & Assert - should throw because BasePath is missing
            Action act = () => AppConfig.LoadFromConfiguration(_tempDirectory);
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*BasePath is missing*");
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
        public void LoadFromConfiguration_WithPartialConfig_ShouldUseDefaultsForMissingValues()
        {
            // Arrange
            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var partialConfig = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{_tempDirectory.Replace("\\", "\\\\")}"",
                    ""FileExtensions"": ["".pdf""]
                }}
            }}";
            File.WriteAllText(configPath, partialConfig);

            // Act
            var result = AppConfig.LoadFromConfiguration(_tempDirectory);

            // Assert
            result.BasePath.Should().Be(_tempDirectory);
            result.FileExtensions.Should().Contain(".pdf");
            result.IncludeSubdirectories.Should().BeTrue(); // Default
            result.ForceReprocessAlreadyNamed.Should().BeFalse(); // Default
            result.MaxDegreeOfParallelism.Should().Be(Environment.ProcessorCount); // Default
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
                    ""LastUsedPath"": """"
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
                    ""FileExtensions"": ["".pdf""]
                }}
            }}";
            File.WriteAllText(configPath, initialConfig);
            
            var appConfig = AppConfig.LoadFromConfiguration(_tempDirectory);

            // Act & Assert
            Action act = () => appConfig.SaveLastUsedPath("/invalid/path");
            act.Should().NotThrow();
        }

        [Fact]
        public void LoadFromConfiguration_WithMissingFileExtensions_ShouldThrowException()
        {
            // Arrange
            var configPath = Path.Combine(_tempDirectory, "appsettings.json");
            var configWithoutExtensions = $@"{{
                ""AppConfig"": {{
                    ""BasePath"": ""{_tempDirectory.Replace("\\", "\\\\")}""
                }}
            }}";
            File.WriteAllText(configPath, configWithoutExtensions);

            // Act & Assert - should throw because FileExtensions is missing
            Action act = () => AppConfig.LoadFromConfiguration(_tempDirectory);
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*FileExtensions*");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Fact]
        public void SaveLastUsedPath_WithEmptyPath_ShouldReturnEarly()
        {
            // Arrange
            var config = new AppConfig();

            // Act & Assert - should not throw
            config.SaveLastUsedPath(string.Empty);
            config.SaveLastUsedPath(null!);
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
    }
}
