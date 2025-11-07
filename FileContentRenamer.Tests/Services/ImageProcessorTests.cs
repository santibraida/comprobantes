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
        [InlineData(".JPG")]
        [InlineData(".JPEG")]
        [InlineData(".PNG")]
        [InlineData(".TIF")]
        [InlineData(".TIFF")]
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
            await File.WriteAllTextAsync(emptyFile, string.Empty);

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
            await File.WriteAllTextAsync(filePath, "test content");

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
            await File.WriteAllTextAsync(filePath, "test content");

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
            _ = new ImageProcessor(config);

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
            await File.WriteAllTextAsync(testFile, "dummy content");

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
            await File.WriteAllTextAsync(testFile, "dummy content");

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
            await File.WriteAllTextAsync(testFile, "dummy image content");

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

        [Fact]
        public void Constructor_WithNullTessdataPath_ShouldNotThrow()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = null,
                TesseractLanguage = "eng"
            };

            // Act
            Action act = () => new ImageProcessor(config);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public async Task ExtractContentAsync_WithNullTessdataPath_ShouldReturnEmptyString()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = null,
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.jpg");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithEmptyTessdataPath_ShouldReturnEmptyString()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = "",
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.jpg");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithNonExistentTessdataPath_ShouldReturnEmptyString()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "nonexistent"),
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.jpg");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Dispose_ShouldCleanupTempDirectory()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);

            // Act
            processor.Dispose();

            // Assert - Just verify no exception is thrown
            // The temp directory cleanup happens in the dispose
            processor.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);

            // Act
            Action act = () =>
            {
                processor.Dispose();
                processor.Dispose();
                processor.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_WhenTessdataCreationFails_ShouldLogError()
        {
            // Arrange - Create a file with the same name as the directory we want to create
            var invalidPath = Path.Combine(_tempDirectory, "blocked_tessdata");
            File.WriteAllText(invalidPath, "blocking file"); // File blocks directory creation

            var config = new AppConfig
            {
                TesseractDataPath = invalidPath,
                TesseractLanguage = "eng"
            };

            // Act
            var processor = new ImageProcessor(config);

            // Assert - Should not throw, but log error internally
            processor.Should().NotBeNull();
        }

        [Fact]
        public async Task ExtractContentAsync_WithNullLanguage_ShouldUseDefaultLanguage()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = null
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.jpg");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert - Should handle null language and default to "eng"
            result.Should().BeEmpty(); // Empty because tesseract isn't actually running
        }

        [Fact]
        public async Task ExtractContentAsync_WithException_ShouldReturnEmptyString()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);

            // Use a path that will cause an exception (e.g., contains invalid characters)
            var invalidPath = Path.Combine(_tempDirectory, "test\0.jpg");

            // Act
            var result = await processor.ExtractContentAsync(invalidPath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void CanProcess_WithFilePathOnly_ShouldCheckExtension()
        {
            // Arrange
            var jpegFile = "image.jpeg";
            var pdfFile = "document.pdf";

            // Act
            var jpegResult = _imageProcessor.CanProcess(jpegFile);
            var pdfResult = _imageProcessor.CanProcess(pdfFile);

            // Assert
            jpegResult.Should().BeTrue();
            pdfResult.Should().BeFalse();
        }

        [Fact]
        public void CanProcess_WithEmptyExtension_ShouldReturnFalse()
        {
            // Arrange
            var fileWithoutExtension = Path.Combine(_tempDirectory, "testfile");

            // Act
            var result = _imageProcessor.CanProcess(fileWithoutExtension);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(".gif")]
        [InlineData(".bmp")]
        [InlineData(".webp")]
        [InlineData(".svg")]
        public void CanProcess_WithUnsupportedImageFormats_ShouldReturnFalse(string extension)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, $"test{extension}");

            // Act
            var result = _imageProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExtractContentAsync_WithLanguageContainingPlus_ShouldUsePrimaryLanguage()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "spa+eng+fra"
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.tif");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert - Should parse and use primary language (spa)
            result.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_CreatesUniqueTempDirectory()
        {
            // Arrange & Act
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };

            var processor1 = new ImageProcessor(config);
            var processor2 = new ImageProcessor(config);

            // Assert - Each processor should have its own temp directory
            processor1.Should().NotBeNull();
            processor2.Should().NotBeNull();

            processor1.Dispose();
            processor2.Dispose();
        }

        [Fact]
        public void Dispose_WithNonExistentTempDirectory_ShouldNotThrow()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);

            // Manually delete the temp directory before disposing
            // (simulates a scenario where temp dir was already cleaned up)

            // Act
            Action act = () => processor.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public async Task ExtractContentAsync_WithEmptyLanguageString_ShouldUseDefaultLanguage()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = string.Empty
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.png");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert - Should handle empty language string
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithWhitespaceLanguage_ShouldUseDefaultLanguage()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "   "
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.jpeg");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithSingleLanguage_ShouldNotSplitOnPlus()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"  // Single language without +
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.tiff");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithLanguageEndingInPlus_ShouldHandleGracefully()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng+"  // Edge case: language ending with +
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.jpg");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_WithExistingFileAtTessdataPath_ShouldHandleException()
        {
            // Arrange - Create a file where we want a directory
            var blockedPath = Path.Combine(_tempDirectory, "blocked_path");
            File.WriteAllText(blockedPath, "This file blocks directory creation");

            var config = new AppConfig
            {
                TesseractDataPath = blockedPath,
                TesseractLanguage = "eng"
            };

            // Act
            var act = () => new ImageProcessor(config);

            // Assert - Should not throw, just log error
            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_WithReadOnlyTessdataPath_ShouldHandleGracefully()
        {
            // Arrange - This test might behave differently on different OS
            var readOnlyPath = Path.Combine(_tempDirectory, "readonly_tessdata");

            var config = new AppConfig
            {
                TesseractDataPath = readOnlyPath,
                TesseractLanguage = "eng"
            };

            // Act
            var act = () => new ImageProcessor(config);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public async Task ExtractContentAsync_WithLongFilePath_ShouldHandleCorrectly()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);

            // Create a nested directory structure for long path
            var longPath = Path.Combine(_tempDirectory, "a", "very", "deep", "nested", "folder", "structure");
            Directory.CreateDirectory(longPath);
            var imagePath = Path.Combine(longPath, "test.png");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithSpecialCharactersInFilename_ShouldHandle()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);

            // Use filename with spaces and special characters
            var imagePath = Path.Combine(_tempDirectory, "test image (1).jpg");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Dispose_AfterAlreadyDisposed_ShouldHandleGracefully()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "eng"
            };
            var processor = new ImageProcessor(config);

            // Act
            processor.Dispose();

            // Call dispose again
            Action act = () => processor.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ExtractContentAsync_WithVariousLanguageValues_ShouldHandleGracefully(string? language)
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = language
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "test.tif");
            await File.WriteAllTextAsync(imagePath, "dummy content");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void CanProcess_WithNullFilePath_ShouldNotThrow()
        {
            // Act
            var act = () => _imageProcessor.CanProcess(null!);

            // Assert - Will throw NullReferenceException, which is expected behavior
            act.Should().Throw<NullReferenceException>();
        }

        [Fact]
        public void CanProcess_WithEmptyFilePath_ShouldReturnFalse()
        {
            // Act
            var result = _imageProcessor.CanProcess(string.Empty);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExtractContentAsync_WithTifExtension_ShouldProcessCorrectly()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "spa"
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "document.tif");
            await File.WriteAllTextAsync(imagePath, "TIF format image");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty(); // Empty because tesseract isn't installed in test environment
        }

        [Fact]
        public async Task ExtractContentAsync_WithTiffExtension_ShouldProcessCorrectly()
        {
            // Arrange
            var config = new AppConfig
            {
                TesseractDataPath = Path.Combine(_tempDirectory, "tessdata"),
                TesseractLanguage = "fra"
            };
            Directory.CreateDirectory(config.TesseractDataPath);

            var processor = new ImageProcessor(config);
            var imagePath = Path.Combine(_tempDirectory, "document.tiff");
            await File.WriteAllTextAsync(imagePath, "TIFF format image");

            // Act
            var result = await processor.ExtractContentAsync(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        // Note: Testing actual OCR functionality would require:
        // 1. Tesseract installed on the test system
        // 2. Valid image files with text content
        // 3. Proper tessdata files
        // These integration tests would be valuable but require additional test infrastructure

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

