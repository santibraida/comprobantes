using FileContentRenamer.Services;
using FluentAssertions;
using Xunit;

namespace FileContentRenamer.Tests.Services
{
    public class TextProcessorTests : IDisposable
    {
        private readonly TextProcessor _textProcessor;
        private readonly string _tempDirectory;

        public TextProcessorTests()
        {
            _textProcessor = new TextProcessor();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public void CanProcess_WithTxtExtension_ShouldReturnTrue()
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, "test.txt");

            // Act
            var result = _textProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("test.TXT")]
        [InlineData("test.Txt")]
        [InlineData("test.tXt")]
        public void CanProcess_WithTxtExtensionDifferentCasing_ShouldReturnTrue(string filename)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, filename);

            // Act
            var result = _textProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("test.pdf")]
        [InlineData("test.jpg")]
        [InlineData("test.docx")]
        [InlineData("test")]
        public void CanProcess_WithNonTxtExtension_ShouldReturnFalse(string filename)
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, filename);

            // Act
            var result = _textProcessor.CanProcess(filePath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExtractContentAsync_WithValidTextFile_ShouldReturnContent()
        {
            // Arrange
            var testContent = "This is test content in a text file";
            var testFile = Path.Combine(_tempDirectory, "test.txt");
            await File.WriteAllTextAsync(testFile, testContent);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().Be(testContent);
        }

        [Fact]
        public async Task ExtractContentAsync_WithEmptyFile_ShouldReturnEmptyString()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "empty.txt");
            await File.WriteAllTextAsync(testFile, string.Empty);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithLargeFile_ShouldReturnFirst1000Characters()
        {
            // Arrange
            var largeContent = new string('A', 1500); // 1500 characters
            var testFile = Path.Combine(_tempDirectory, "large.txt");
            await File.WriteAllTextAsync(testFile, largeContent);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().HaveLength(1000);
            result.Should().Be(new string('A', 1000));
        }

        [Fact]
        public async Task ExtractContentAsync_WithExactly1000Characters_ShouldReturnAllContent()
        {
            // Arrange
            var content = new string('B', 1000); // Exactly 1000 characters
            var testFile = Path.Combine(_tempDirectory, "exact1000.txt");
            await File.WriteAllTextAsync(testFile, content);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().HaveLength(1000);
            result.Should().Be(content);
        }

        [Fact]
        public async Task ExtractContentAsync_WithMultilineContent_ShouldReturnContent()
        {
            // Arrange
            var testContent = "Line 1\nLine 2\rLine 3\r\nLine 4";
            var testFile = Path.Combine(_tempDirectory, "multiline.txt");
            await File.WriteAllTextAsync(testFile, testContent);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().Be(testContent);
        }

        [Fact]
        public async Task ExtractContentAsync_WithSpecialCharacters_ShouldReturnContent()
        {
            // Arrange
            var testContent = "Special chars: áéíóú ñÑ üÜ ¿¡ €$@#%";
            var testFile = Path.Combine(_tempDirectory, "special.txt");
            await File.WriteAllTextAsync(testFile, testContent);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().Be(testContent);
        }

        [Fact]
        public async Task ExtractContentAsync_WithNonExistentFile_ShouldReturnEmptyString()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.txt");

            // Act
            var result = await _textProcessor.ExtractContentAsync(nonExistentFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithLockedFile_ShouldReturnEmptyString()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "locked.txt");
            await File.WriteAllTextAsync(testFile, "test content");

            using var fileStream = File.Open(testFile, FileMode.Open, FileAccess.Read, FileShare.None);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractContentAsync_WithUnicodeContent_ShouldReturnContent()
        {
            // Arrange
            var unicodeContent = "Unicode: 中文 🎉 ❤️ ⭐ 😊";
            var testFile = Path.Combine(_tempDirectory, "unicode.txt");
            await File.WriteAllTextAsync(testFile, unicodeContent, System.Text.Encoding.UTF8);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().Be(unicodeContent);
        }

        [Fact]
        public async Task ExtractContentAsync_WithOnlyWhitespace_ShouldReturnWhitespace()
        {
            // Arrange
            var whitespaceContent = "   \t\n\r   ";
            var testFile = Path.Combine(_tempDirectory, "whitespace.txt");
            await File.WriteAllTextAsync(testFile, whitespaceContent);

            // Act
            var result = await _textProcessor.ExtractContentAsync(testFile);

            // Assert
            result.Should().Be(whitespaceContent);
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
