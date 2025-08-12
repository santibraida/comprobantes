using FileContentRenamer.Models;
using FileContentRenamer.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace FileContentRenamer.Tests.Services
{
    public class FileServiceTests : IDisposable
    {
        private readonly Mock<IDateExtractor> _dateExtractorMock;
        private readonly Mock<IDirectoryOrganizer> _directoryOrganizerMock;
        private readonly Mock<IFilenameGenerator> _filenameGeneratorMock;
        private readonly Mock<IFileValidator> _fileValidatorMock;
        private readonly Mock<IFileProcessor> _processorMock;
        private readonly AppConfig _appConfig;
        private readonly FileService _fileService;
        private readonly string _tempDirectory;

        public FileServiceTests()
        {
            _dateExtractorMock = new Mock<IDateExtractor>();
            _directoryOrganizerMock = new Mock<IDirectoryOrganizer>();
            _filenameGeneratorMock = new Mock<IFilenameGenerator>();
            _fileValidatorMock = new Mock<IFileValidator>();
            _processorMock = new Mock<IFileProcessor>();

            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            _appConfig = new AppConfig
            {
                BasePath = _tempDirectory,
                FileExtensions = new List<string> { ".pdf", ".txt" },
                IncludeSubdirectories = false,
                MaxDegreeOfParallelism = 1
            };

            var processors = new List<IFileProcessor> { _processorMock.Object };

            _fileService = new FileService(
                _appConfig,
                processors,
                _dateExtractorMock.Object,
                _directoryOrganizerMock.Object,
                _filenameGeneratorMock.Object,
                _fileValidatorMock.Object
            );
        }

        [Fact]
        public async Task ProcessFilesAsync_WithInvalidConfiguration_ShouldReturnEarly()
        {
            // Arrange
            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(false);

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _fileValidatorMock.Verify(x => x.ValidateConfiguration(), Times.Once);
        }

        [Fact]
        public async Task ProcessFilesAsync_WithNoFiles_ShouldCompleteSuccessfully()
        {
            // Arrange
            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _fileValidatorMock.Verify(x => x.ValidateConfiguration(), Times.Once);
        }

        [Fact]
        public async Task ProcessFilesAsync_WithValidFiles_ShouldProcessFiles()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldProcessFile(testFile)).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldSkipAlreadyNamedFile(testFile)).Returns(false);
            _fileValidatorMock.Setup(x => x.ValidateFileSize(testFile));

            _processorMock.Setup(x => x.CanProcess(testFile)).Returns(true);
            _processorMock.Setup(x => x.ExtractContentAsync(testFile)).ReturnsAsync("extracted content");

            _filenameGeneratorMock.Setup(x => x.GenerateFilename("extracted content", testFile))
                .Returns("new_filename.pdf");
            _filenameGeneratorMock.Setup(x => x.GenerateUniqueFilename(_tempDirectory, "new_filename.pdf"))
                .Returns(Path.Combine(_tempDirectory, "new_filename.pdf"));

            _dateExtractorMock.Setup(x => x.ExtractDateFromContent("extracted content"))
                .Returns("2024-03-15");

            _directoryOrganizerMock.Setup(x => x.OrganizeFileIntoDirectoryStructure(It.IsAny<string>(), "2024-03-15"))
                .Returns(Path.Combine(_tempDirectory, "organized_file.pdf"));

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _processorMock.Verify(x => x.ExtractContentAsync(testFile), Times.Once);
            _filenameGeneratorMock.Verify(x => x.GenerateFilename("extracted content", testFile), Times.Once);
        }

        [Fact]
        public async Task ProcessFilesAsync_WithAlreadyNamedFile_ShouldSkipRenaming()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "service_2024-03-15_payment.pdf");
            File.WriteAllText(testFile, "test content");

            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldProcessFile(testFile)).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldSkipAlreadyNamedFile(testFile)).Returns(true);

            _dateExtractorMock.Setup(x => x.ExtractDateFromFilename("service_2024-03-15_payment"))
                .Returns("2024-03-15");
            _directoryOrganizerMock.Setup(x => x.OrganizeFileIntoDirectoryStructure(testFile, "2024-03-15"))
                .Returns(testFile);

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _dateExtractorMock.Verify(x => x.ExtractDateFromFilename("service_2024-03-15_payment"), Times.Once);
            _directoryOrganizerMock.Verify(x => x.OrganizeFileIntoDirectoryStructure(testFile, "2024-03-15"), Times.Once);
            _processorMock.Verify(x => x.ExtractContentAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessFilesAsync_WithNoProcessorAvailable_ShouldSkipFile()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.unknown");
            File.WriteAllText(testFile, "test content");

            _appConfig.FileExtensions = new List<string> { ".unknown" };

            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldProcessFile(testFile)).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldSkipAlreadyNamedFile(testFile)).Returns(false);
            _fileValidatorMock.Setup(x => x.ValidateFileSize(testFile));

            _processorMock.Setup(x => x.CanProcess(testFile)).Returns(false);

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _processorMock.Verify(x => x.ExtractContentAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessFilesAsync_WithEmptyContent_ShouldSkipFile()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldProcessFile(testFile)).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldSkipAlreadyNamedFile(testFile)).Returns(false);
            _fileValidatorMock.Setup(x => x.ValidateFileSize(testFile));

            _processorMock.Setup(x => x.CanProcess(testFile)).Returns(true);
            _processorMock.Setup(x => x.ExtractContentAsync(testFile)).ReturnsAsync(string.Empty);

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _filenameGeneratorMock.Verify(x => x.GenerateFilename(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessFilesAsync_WithSameFilename_ShouldOrganizeOnly()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldProcessFile(testFile)).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldSkipAlreadyNamedFile(testFile)).Returns(false);
            _fileValidatorMock.Setup(x => x.ValidateFileSize(testFile));

            _processorMock.Setup(x => x.CanProcess(testFile)).Returns(true);
            _processorMock.Setup(x => x.ExtractContentAsync(testFile)).ReturnsAsync("extracted content");

            _filenameGeneratorMock.Setup(x => x.GenerateFilename("extracted content", testFile))
                .Returns("test.pdf"); // Same as original

            _dateExtractorMock.Setup(x => x.ExtractDateFromContent("extracted content"))
                .Returns("2024-03-15");
            _directoryOrganizerMock.Setup(x => x.OrganizeFileIntoDirectoryStructure(testFile, "2024-03-15"))
                .Returns(Path.Combine(_tempDirectory, "2024", "03", "test.pdf"));

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _filenameGeneratorMock.Verify(x => x.GenerateUniqueFilename(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _directoryOrganizerMock.Verify(x => x.OrganizeFileIntoDirectoryStructure(testFile, "2024-03-15"), Times.Once);
        }

        [Fact]
        public async Task ProcessFilesAsync_WithSubdirectories_ShouldProcessAllFiles()
        {
            // Arrange
            var subDir = Path.Combine(_tempDirectory, "subdir");
            Directory.CreateDirectory(subDir);
            
            var testFile1 = Path.Combine(_tempDirectory, "test1.pdf");
            var testFile2 = Path.Combine(subDir, "test2.pdf");
            
            File.WriteAllText(testFile1, "test content 1");
            File.WriteAllText(testFile2, "test content 2");

            _appConfig.IncludeSubdirectories = true;

            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldProcessFile(It.IsAny<string>())).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldSkipAlreadyNamedFile(It.IsAny<string>())).Returns(false);
            _fileValidatorMock.Setup(x => x.ValidateFileSize(It.IsAny<string>()));

            _processorMock.Setup(x => x.CanProcess(It.IsAny<string>())).Returns(true);
            _processorMock.Setup(x => x.ExtractContentAsync(It.IsAny<string>())).ReturnsAsync("content");

            _filenameGeneratorMock.Setup(x => x.GenerateFilename(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("new_filename.pdf");

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _processorMock.Verify(x => x.ExtractContentAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessFilesAsync_WithParallelProcessing_ShouldProcessMultipleFiles()
        {
            // Arrange
            _appConfig.MaxDegreeOfParallelism = 2;

            var testFiles = new[]
            {
                Path.Combine(_tempDirectory, "test1.pdf"),
                Path.Combine(_tempDirectory, "test2.pdf"),
                Path.Combine(_tempDirectory, "test3.pdf")
            };

            foreach (var file in testFiles)
            {
                File.WriteAllText(file, "test content");
            }

            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldProcessFile(It.IsAny<string>())).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldSkipAlreadyNamedFile(It.IsAny<string>())).Returns(false);
            _fileValidatorMock.Setup(x => x.ValidateFileSize(It.IsAny<string>()));

            _processorMock.Setup(x => x.CanProcess(It.IsAny<string>())).Returns(true);
            _processorMock.Setup(x => x.ExtractContentAsync(It.IsAny<string>())).ReturnsAsync("content");

            _filenameGeneratorMock.Setup(x => x.GenerateFilename(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("new_filename.pdf");

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _processorMock.Verify(x => x.ExtractContentAsync(It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ProcessFilesAsync_WithException_ShouldContinueProcessing()
        {
            // Arrange
            var testFile1 = Path.Combine(_tempDirectory, "test1.pdf");
            var testFile2 = Path.Combine(_tempDirectory, "test2.pdf");
            
            File.WriteAllText(testFile1, "test content 1");
            File.WriteAllText(testFile2, "test content 2");

            _fileValidatorMock.Setup(x => x.ValidateConfiguration()).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldProcessFile(It.IsAny<string>())).Returns(true);
            _fileValidatorMock.Setup(x => x.ShouldSkipAlreadyNamedFile(It.IsAny<string>())).Returns(false);
            _fileValidatorMock.Setup(x => x.ValidateFileSize(testFile1)).Throws(new Exception("Test exception"));
            _fileValidatorMock.Setup(x => x.ValidateFileSize(testFile2));

            _processorMock.Setup(x => x.CanProcess(testFile2)).Returns(true);
            _processorMock.Setup(x => x.ExtractContentAsync(testFile2)).ReturnsAsync("content");

            // Act
            await _fileService.ProcessFilesAsync();

            // Assert
            _processorMock.Verify(x => x.ExtractContentAsync(testFile2), Times.Once);
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
