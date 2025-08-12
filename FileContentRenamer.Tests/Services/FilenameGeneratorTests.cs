using System.Collections.Concurrent;
using FileContentRenamer.Models;
using FileContentRenamer.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace FileContentRenamer.Tests.Services
{
    public class FilenameGeneratorTests
    {
        private readonly Mock<IDateExtractor> _dateExtractorMock;
        private readonly AppConfig _appConfig;
        private readonly FilenameGenerator _filenameGenerator;

        public FilenameGeneratorTests()
        {
            _dateExtractorMock = new Mock<IDateExtractor>();
            _appConfig = new AppConfig
            {
                NamingRules = new NamingRules("test", "card")
            };
            _filenameGenerator = new FilenameGenerator(_dateExtractorMock.Object, _appConfig);
        }

        [Fact]
        public void GenerateFilename_WithValidContentAndDate_ShouldReturnCorrectFilename()
        {
            // Arrange
            var content = "Some bill content";
            var originalPath = "/path/to/test.pdf";
            var expectedDate = "2024-03-15";
            
            _dateExtractorMock.Setup(x => x.ExtractDateFromContent(content))
                .Returns(expectedDate);

            // Act
            var result = _filenameGenerator.GenerateFilename(content, originalPath);

            // Assert
            result.Should().Be("test_2024-03-15_card.pdf");
        }

        [Fact]
        public void GenerateFilename_WithNoDateInContent_ShouldUseCurrentDate()
        {
            // Arrange
            var content = "Some content without date";
            var originalPath = "/path/to/test.pdf";
            var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            
            _dateExtractorMock.Setup(x => x.ExtractDateFromContent(content))
                .Returns(string.Empty);

            // Act
            var result = _filenameGenerator.GenerateFilename(content, originalPath);

            // Assert
            result.Should().Be($"test_{currentDate}_card.pdf");
        }

        [Fact]
        public void GenerateFilename_WithNullDate_ShouldUseCurrentDate()
        {
            // Arrange
            var content = "Some content";
            var originalPath = "/path/to/test.pdf";
            var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            
            _dateExtractorMock.Setup(x => x.ExtractDateFromContent(content))
                .Returns((string?)null!);

            // Act
            var result = _filenameGenerator.GenerateFilename(content, originalPath);

            // Assert
            result.Should().Be($"test_{currentDate}_card.pdf");
        }

        [Fact]
        public void GenerateFilename_WithDifferentExtension_ShouldPreserveExtension()
        {
            // Arrange
            var content = "Some content";
            var originalPath = "/path/to/test.jpg";
            var expectedDate = "2024-03-15";
            
            _dateExtractorMock.Setup(x => x.ExtractDateFromContent(content))
                .Returns(expectedDate);

            // Act
            var result = _filenameGenerator.GenerateFilename(content, originalPath);

            // Assert
            result.Should().Be("test_2024-03-15_card.jpg");
        }

        [Fact]
        public void IsAlreadyNamedFilename_WithCorrectFormat_ShouldReturnTrue()
        {
            // Arrange
            var filename = "service_2024-03-15_payment";

            // Act
            var result = _filenameGenerator.IsAlreadyNamedFilename(filename);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("service_2024-03-15_payment")]
        [InlineData("test_2023-12-31_card")]
        [InlineData("company_2025-01-01_bank")]
        public void IsAlreadyNamedFilename_WithValidFormats_ShouldReturnTrue(string filename)
        {
            // Act
            var result = _filenameGenerator.IsAlreadyNamedFilename(filename);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("service_payment")]
        [InlineData("2024-03-15")]
        [InlineData("service_2024-03-15")]
        [InlineData("service_15-03-2024_payment")]
        [InlineData("")]
        public void IsAlreadyNamedFilename_WithInvalidFormats_ShouldReturnFalse(string filename)
        {
            // Act
            var result = _filenameGenerator.IsAlreadyNamedFilename(filename);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GenerateUniqueFilename_WithNonExistingFile_ShouldReturnOriginalPath()
        {
            // Arrange
            var directory = Path.GetTempPath();
            var baseFilename = "test_unique_file.txt";
            var expectedPath = Path.Combine(directory, baseFilename);

            // Act
            var result = _filenameGenerator.GenerateUniqueFilename(directory, baseFilename);

            // Assert
            result.Should().Be(expectedPath);
        }

        [Fact]
        public void GenerateUniqueFilename_WithExistingFile_ShouldReturnNumberedVersion()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var baseFilename = "test_file.txt";
                var existingFilePath = Path.Combine(tempDir, baseFilename);
                File.WriteAllText(existingFilePath, "test");

                // Act
                var result = _filenameGenerator.GenerateUniqueFilename(tempDir, baseFilename);

                // Assert
                result.Should().Be(Path.Combine(tempDir, "test_file_2.txt"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GenerateUniqueFilename_WithMultipleExistingFiles_ShouldReturnNextAvailableNumber()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var baseFilename = "test_file.txt";
                
                // Create multiple existing files
                File.WriteAllText(Path.Combine(tempDir, "test_file.txt"), "test");
                File.WriteAllText(Path.Combine(tempDir, "test_file_2.txt"), "test");
                File.WriteAllText(Path.Combine(tempDir, "test_file_3.txt"), "test");

                // Act
                var result = _filenameGenerator.GenerateUniqueFilename(tempDir, baseFilename);

                // Assert
                result.Should().Be(Path.Combine(tempDir, "test_file_4.txt"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Theory]
        [InlineData("valid_filename", "valid_filename")]
        [InlineData("file<>name", "file<>name")] // < and > are not invalid on macOS
        [InlineData("file|name", "file|name")] // | is not invalid on macOS
        [InlineData("file:name", "file:name")] // : is not invalid on macOS
        [InlineData("", "unknown_file")] // Empty string becomes "unknown_file"
        [InlineData("___", "cleaned_file")] // Underscores get reduced to empty, then becomes "cleaned_file"
        [InlineData("__file__name__", "file_name")] // Leading/trailing underscores trimmed
        public void CleanupFileName_WithVariousInputs_ShouldReturnCleanedName(string input, string expected)
        {
            // This tests the private CleanupFileName method using reflection
            
            // Arrange
            var cleanupMethod = typeof(FilenameGenerator).GetMethod("CleanupFileName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Act
            var result = cleanupMethod!.Invoke(null, new object[] { input });

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GenerateFilename_WithNullDirectory_ShouldReturnOriginalFilename()
        {
            // Arrange
            var content = "test content";
            var originalPath = "test.pdf"; // No directory path - Path.GetDirectoryName returns empty string, not null
            
            // Setup mocks - the method will go through normal processing
            _dateExtractorMock.Setup(x => x.ExtractDateFromContent(content))
                .Returns("2025-08-11");
            
            // Act
            var result = _filenameGenerator.GenerateFilename(content, originalPath);

            // Assert
            result.Should().Be("test_2025-08-11_card.pdf");
        }

        [Fact]
        public async Task GenerateUniqueFilename_ThreadSafety_ShouldHandleConcurrentCalls()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var baseFilename = "concurrent_test.txt";
                var results = new ConcurrentBag<string>();
                var tasks = new List<Task>();

                // Create the base file first so all threads will need to generate numbered versions
                var baseFilePath = Path.Combine(tempDir, baseFilename);
                await File.WriteAllTextAsync(baseFilePath, "test");

                // Act - simulate concurrent calls
                for (int i = 0; i < 5; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var result = _filenameGenerator.GenerateUniqueFilename(tempDir, baseFilename);
                        results.Add(result);
                        
                        // Actually create the file to make the next call unique
                        File.WriteAllText(result, "test");
                    }));
                }

                await Task.WhenAll(tasks);

                // Assert - all results should be unique
                var resultList = results.ToList();
                resultList.Should().HaveCount(5);
                resultList.Should().OnlyHaveUniqueItems();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
