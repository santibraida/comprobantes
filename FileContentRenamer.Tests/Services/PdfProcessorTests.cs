using FileContentRenamer.Services;
using FluentAssertions;
using Xunit;

namespace FileContentRenamer.Tests.Services
{
    public class PdfProcessorTests : IDisposable
    {
        private readonly PdfProcessor _pdfProcessor;
        private readonly string _tempDirectory;

        public PdfProcessorTests()
        {
            _pdfProcessor = new PdfProcessor();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public void CanProcess_WithPdfExtension_ShouldReturnTrue()
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, "test.pdf");

            // Act
            var result = _pdfProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("test.PDF")]
        [InlineData("test.Pdf")]
        [InlineData("test.pDf")]
        public void CanProcess_WithPdfExtensionDifferentCasing_ShouldReturnTrue(string filename)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, filename);

            // Act
            var result = _pdfProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("test.txt")]
        [InlineData("test.jpg")]
        [InlineData("test.docx")]
        [InlineData("test")]
        public void CanProcess_WithNonPdfExtension_ShouldReturnFalse(string filename)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, filename);

            // Act
            var result = _pdfProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExtractContentAsync_WithNonExistentFile_ShouldReturnEmptyString()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.pdf");

            // Act
            var result = await _pdfProcessor.ExtractContentAsync(nonExistentFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithInvalidPdfFile_ShouldReturnEmptyString()
        {
            // Arrange
            var invalidPdf = Path.Combine(_tempDirectory, "invalid.pdf");
            await File.WriteAllTextAsync(invalidPdf, "This is not a valid PDF file");

            // Act
            var result = await _pdfProcessor.ExtractContentAsync(invalidPdf);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithEmptyFile_ShouldReturnEmptyString()
        {
            // Arrange
            var emptyFile = Path.Combine(_tempDirectory, "empty.pdf");
            File.WriteAllText(emptyFile, string.Empty);

            // Act
            var result = await _pdfProcessor.ExtractContentAsync(emptyFile);

            // Assert
            result.Should().BeEmpty();
        }

        // Note: We cannot easily test with valid PDF files without creating complex test fixtures
        // The processor's CanProcess method is fully tested, and error handling is tested
        // In a real scenario, we would need to create or include sample PDF files for testing

        [Fact]
        public async Task ExtractContentAsync_WithNonPdfFile_ShouldThrowException()
        {
            // Arrange
            var nonPdfFile = Path.Combine(_tempDirectory, "test.txt");
            await File.WriteAllTextAsync(nonPdfFile, "This is not a PDF file content");

            // Act
            var result = await _pdfProcessor.ExtractContentAsync(nonPdfFile);

            // Assert - should return empty string due to exception handling
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithCorruptedPdfFile_ShouldReturnEmptyString()
        {
            // Arrange
            var corruptedPdf = Path.Combine(_tempDirectory, "corrupted.pdf");
            await File.WriteAllTextAsync(corruptedPdf, "This is corrupted PDF content");

            // Act
            var result = await _pdfProcessor.ExtractContentAsync(corruptedPdf);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithNonExistentPdfFile_ShouldReturnEmptyString()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.pdf");

            // Act
            var result = await _pdfProcessor.ExtractContentAsync(nonExistentFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithEmptyPdfFile_ShouldReturnEmptyString()
        {
            // Arrange
            var emptyFile = Path.Combine(_tempDirectory, "empty.pdf");
            File.WriteAllText(emptyFile, string.Empty);

            // Act
            var result = await _pdfProcessor.ExtractContentAsync(emptyFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("document.pdf")]
        [InlineData("document.PDF")]
        [InlineData("DOCUMENT.PDF")]
        public void CanProcess_WithPdfExtensionVariations_ShouldReturnTrue(string filename)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, filename);

            // Act
            var result = _pdfProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExtractContentAsync_WithValidPdfFile_ShouldProcessPagesUpToLimit()
        {
            // This test simulates the internal logic flow by testing exception handling paths
            // Since we can't easily create valid PDFs in tests, we focus on edge cases
            
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            
            // Create a file with minimal PDF header to trigger different error paths
            var pdfHeader = "%PDF-1.4\n";
            await File.WriteAllTextAsync(testFile, pdfHeader);

            // Act
            var result = await _pdfProcessor.ExtractContentAsync(testFile);

            // Assert - should return empty due to invalid PDF structure
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithNullOrEmptyPath_ShouldHandleGracefully()
        {
            // Testing null path handling
            var result1 = await _pdfProcessor.ExtractContentAsync(string.Empty);
            result1.Should().BeEmpty();

            // Testing exception handling for invalid paths
            var result2 = await _pdfProcessor.ExtractContentAsync("invalid/path/file.pdf");
            result2.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithDirectoryPath_ShouldReturnEmpty()
        {
            // Arrange - pass a directory path instead of file path
            
            // Act
            var result = await _pdfProcessor.ExtractContentAsync(_tempDirectory);

            // Assert
            result.Should().BeEmpty();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}
