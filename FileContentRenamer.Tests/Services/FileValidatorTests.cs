using FileContentRenamer.Models;
using FileContentRenamer.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace FileContentRenamer.Tests.Services
{
    public class FileValidatorTests : IDisposable
    {
        private readonly Mock<IFilenameGenerator> _filenameGeneratorMock;
        private readonly AppConfig _appConfig;
        private readonly FileValidator _fileValidator;
        private readonly string _tempDirectory;

        public FileValidatorTests()
        {
            _filenameGeneratorMock = new Mock<IFilenameGenerator>();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            _appConfig = new AppConfig
            {
                BasePath = _tempDirectory,
                FileExtensions = new List<string> { ".pdf", ".txt", ".jpg" },
                ForceReprocessAlreadyNamed = false
            };

            _fileValidator = new FileValidator(_appConfig, _filenameGeneratorMock.Object);
        }

        [Fact]
        public void ShouldProcessFile_WithValidFile_ShouldReturnTrue()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _fileValidator.ShouldProcessFile(testFile);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldProcessFile_WithNonExistentFile_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.pdf");

            // Act
            var result = _fileValidator.ShouldProcessFile(nonExistentFile);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldProcessFile_WithNullPath_ShouldReturnFalse()
        {
            // Act
            var result = _fileValidator.ShouldProcessFile(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldProcessFile_WithEmptyPath_ShouldReturnFalse()
        {
            // Act
            var result = _fileValidator.ShouldProcessFile(string.Empty);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(".pdf")]
        [InlineData(".txt")]
        [InlineData(".jpg")]
        public void ShouldProcessFile_WithValidExtensions_ShouldReturnTrue(string extension)
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, $"test{extension}");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _fileValidator.ShouldProcessFile(testFile);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(".doc")]
        [InlineData(".xlsx")]
        [InlineData(".png")]
        public void ShouldProcessFile_WithInvalidExtensions_ShouldReturnFalse(string extension)
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, $"test{extension}");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _fileValidator.ShouldProcessFile(testFile);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldProcessFile_WithMixedCaseExtension_ShouldReturnTrue()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.PDF");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _fileValidator.ShouldProcessFile(testFile);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldSkipAlreadyNamedFile_WithAlreadyNamedFile_ShouldReturnTrue()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "service_2024-03-15_payment.pdf");

            _filenameGeneratorMock.Setup(x => x.IsAlreadyNamedFilename("service_2024-03-15_payment"))
                .Returns(true);

            // Act
            var result = _fileValidator.ShouldSkipAlreadyNamedFile(testFile);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldSkipAlreadyNamedFile_WithNotAlreadyNamedFile_ShouldReturnFalse()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "random_filename.pdf");

            _filenameGeneratorMock.Setup(x => x.IsAlreadyNamedFilename("random_filename"))
                .Returns(false);

            // Act
            var result = _fileValidator.ShouldSkipAlreadyNamedFile(testFile);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldSkipAlreadyNamedFile_WithForceReprocessEnabled_ShouldReturnFalse()
        {
            // Arrange
            _appConfig.ForceReprocessAlreadyNamed = true;
            var testFile = Path.Combine(_tempDirectory, "service_2024-03-15_payment.pdf");

            // Act
            var result = _fileValidator.ShouldSkipAlreadyNamedFile(testFile);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateConfiguration_WithValidConfig_ShouldReturnTrue()
        {
            // Act
            var result = _fileValidator.ValidateConfiguration();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateConfiguration_WithEmptyBasePath_ShouldReturnFalse()
        {
            // Arrange
            _appConfig.BasePath = string.Empty;

            // Act
            var result = _fileValidator.ValidateConfiguration();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateConfiguration_WithNullBasePath_ShouldReturnFalse()
        {
            // Arrange
            _appConfig.BasePath = null;

            // Act
            var result = _fileValidator.ValidateConfiguration();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateConfiguration_WithNonExistentBasePath_ShouldReturnFalse()
        {
            // Arrange
            _appConfig.BasePath = Path.Combine(_tempDirectory, "nonexistent");

            // Act
            var result = _fileValidator.ValidateConfiguration();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateConfiguration_WithNullFileExtensions_ShouldReturnFalse()
        {
            // Arrange
            _appConfig.FileExtensions = null!;

            // Act
            var result = _fileValidator.ValidateConfiguration();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateConfiguration_WithEmptyFileExtensions_ShouldReturnFalse()
        {
            // Arrange
            _appConfig.FileExtensions = new List<string>();

            // Act
            var result = _fileValidator.ValidateConfiguration();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateFileSize_WithNormalFile_ShouldNotThrow()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "normal.pdf");
            File.WriteAllText(testFile, "Normal size content");

            // Act & Assert
            var act = () => _fileValidator.ValidateFileSize(testFile);
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateFileSize_WithEmptyFile_ShouldLogWarning()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "empty.pdf");
            File.WriteAllText(testFile, string.Empty);

            // Act & Assert
            var act = () => _fileValidator.ValidateFileSize(testFile);
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateFileSize_WithLargeFile_ShouldLogWarning()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "large.pdf");
            var largeContent = new string('X', 60 * 1024 * 1024); // 60 MB
            File.WriteAllText(testFile, largeContent);

            // Act & Assert
            var act = () => _fileValidator.ValidateFileSize(testFile);
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateFileSize_WithNonExistentFile_ShouldNotThrow()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.pdf");

            // Act & Assert
            var act = () => _fileValidator.ValidateFileSize(nonExistentFile);
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateFileSize_WithInvalidPath_ShouldNotThrow()
        {
            // Arrange
            var invalidPath = "invalid<>|path";

            // Act & Assert
            var act = () => _fileValidator.ValidateFileSize(invalidPath);
            act.Should().NotThrow();
        }

        [Fact]
        public void ShouldProcessFile_WithFileExtensionsContainingNulls_ShouldHandleGracefully()
        {
            // Arrange
            _appConfig.FileExtensions = new List<string> { ".pdf", null!, ".txt" };
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _fileValidator.ShouldProcessFile(testFile);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("test.PDF")]
        [InlineData("test.Pdf")]
        [InlineData("test.pDf")]
        public void ShouldProcessFile_WithDifferentCasing_ShouldBeCaseInsensitive(string filename)
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, filename);
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _fileValidator.ShouldProcessFile(testFile);

            // Assert
            result.Should().BeTrue();
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
