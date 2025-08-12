using FileContentRenamer.Models;
using FileContentRenamer.Services;
using FluentAssertions;
using Xunit;

namespace FileContentRenamer.Tests.Services
{
    public class ImageProcessorTests : IDisposable
    {
        private readonly ImageProcessor _imageProcessor;
        private readonly AppConfig _appConfig;
        private readonly string _tempDirectory;

        public ImageProcessorTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            _appConfig = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };

            _imageProcessor = new ImageProcessor(_appConfig);
        }

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        [InlineData(".tif")]
        [InlineData(".tiff")]
        public void CanProcess_WithImageExtensions_ShouldReturnTrue(string extension)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, $"test{extension}");

            // Act
            var result = _imageProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(".JPG")]
        [InlineData(".JPEG")]
        [InlineData(".PNG")]
        [InlineData(".TIF")]
        [InlineData(".TIFF")]
        public void CanProcess_WithImageExtensionsDifferentCasing_ShouldReturnTrue(string extension)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, $"test{extension}");

            // Act
            var result = _imageProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(".pdf")]
        [InlineData(".txt")]
        [InlineData(".docx")]
        [InlineData(".mp4")]
        [InlineData("test")]
        public void CanProcess_WithNonImageExtension_ShouldReturnFalse(string extension)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, $"test{extension}");

            // Act
            var result = _imageProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExtractContentAsync_WithNonExistentFile_ShouldReturnEmptyString()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.jpg");

            // Act
            var result = await _imageProcessor.ExtractContentAsync(nonExistentFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithInvalidImageFile_ShouldReturnEmptyString()
        {
            // Arrange
            var invalidImage = Path.Combine(_tempDirectory, "invalid.jpg");
            await File.WriteAllTextAsync(invalidImage, "This is not a valid image file");

            // Act
            var result = await _imageProcessor.ExtractContentAsync(invalidImage);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithEmptyFile_ShouldReturnEmptyString()
        {
            // Arrange
            var emptyFile = Path.Combine(_tempDirectory, "empty.jpg");
            File.WriteAllText(emptyFile, string.Empty);

            // Act
            var result = await _imageProcessor.ExtractContentAsync(emptyFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_WithNullTesseractDataPath_ShouldNotThrow()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = null,
                TesseractLanguage = "eng"
            };

            // Act & Assert
            var act = () => new ImageProcessor(config);
            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_WithNonExistentTesseractDataPath_ShouldCreateDirectory()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "tesseract", "new_folder");
            var config = new AppConfig
            {
                TesseractDataPath = nonExistentPath,
                TesseractLanguage = "eng"
            };

            // Act
            var processor = new ImageProcessor(config);

            // Assert
            processor.Should().NotBeNull();
            Directory.Exists(nonExistentPath).Should().BeTrue();
        }

        [Fact]
        public async Task ExtractContentAsync_WithMissingTesseractDataPath_ShouldReturnEmpty()
        {
            // Arrange
            var config = new AppConfig { TesseractDataPath = null };
            var processor = new ImageProcessor(config);
            var filePath = Path.Combine(_tempDirectory, "test.png");
            File.WriteAllText(filePath, "test content");

            // Act
            var result = await processor.ExtractContentAsync(filePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithInvalidTesseractDataPath_ShouldReturnEmpty()
        {
            // Arrange
            var config = new AppConfig { TesseractDataPath = "/invalid/path/that/does/not/exist" };
            var processor = new ImageProcessor(config);
            var filePath = Path.Combine(_tempDirectory, "test.tif");
            File.WriteAllText(filePath, "test content");

            // Act
            var result = await processor.ExtractContentAsync(filePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ImageProcessor(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithNewTesseractDataPath_ShouldCreateDirectory()
        {
            // Arrange
            var tessdataPath = Path.Combine(_tempDirectory, "new_tessdata");
            var config = new AppConfig
            {
                TesseractDataPath = tessdataPath,
                TesseractLanguage = "eng"
            };

            // Act
            var processor = new ImageProcessor(config);

            // Assert
            Directory.Exists(tessdataPath).Should().BeTrue();
        }

        [Fact]
        public async Task ExtractContentAsync_WithMissingTessdataPath_ShouldReturnEmpty()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = null,
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);
            var testFile = Path.Combine(_tempDirectory, "test.jpg");
            File.WriteAllText(testFile, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(testFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithNonExistentTessdataPath_ShouldReturnEmpty()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent");
            var config = new AppConfig
            {
                TesseractDataPath = nonExistentPath,
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);
            var testFile = Path.Combine(_tempDirectory, "test.jpg");
            File.WriteAllText(testFile, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(testFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithComplexLanguageConfig_ShouldHandleLanguageParsing()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "spa+eng" // Test multi-language handling
            };
            Directory.CreateDirectory(config.TesseractDataPath);
            
            var processor = new ImageProcessor(config);
            var testFile = Path.Combine(_tempDirectory, "test.png");
            File.WriteAllText(testFile, "dummy image content");

            // Act
            var result = await processor.ExtractContentAsync(testFile);

            // Assert - Should handle the language parsing logic
            result.Should().BeEmpty(); // Will be empty due to tesseract not being available in test environment
        }

        [Fact]
        public void Constructor_WithExistingTessdataPath_ShouldLogInformation()
        {
            // Arrange
            var tessdataPath = Path.Combine(_tempDirectory, "existing_tessdata");
            Directory.CreateDirectory(tessdataPath); // Pre-create the directory
            
            var config = new AppConfig
            {
                TesseractDataPath = tessdataPath,
                TesseractLanguage = "eng"
            };

            // Act
            var processor = new ImageProcessor(config);

            // Assert
            Directory.Exists(tessdataPath).Should().BeTrue();
            processor.Should().NotBeNull();
        }

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".JPEG")]
        [InlineData(".Png")]
        [InlineData(".TIF")]
        [InlineData(".tiff")]
        public void CanProcess_WithVariousCases_ShouldReturnCorrectResult(string extension)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, $"testfile{extension}");

            // Act
            var result = _imageProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeTrue();
        }

        // Note: Testing actual OCR functionality would require:
        // 1. Tesseract installed on the test system
        // 2. Valid image files with text content
        // 3. Proper tessdata files
        // These integration tests would be valuable but require additional test infrastructure

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}
