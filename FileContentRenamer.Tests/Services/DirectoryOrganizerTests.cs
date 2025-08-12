using FileContentRenamer.Services;
using FluentAssertions;
using Xunit;

namespace FileContentRenamer.Tests.Services
{
    public class DirectoryOrganizerTests : IDisposable
    {
        private readonly DirectoryOrganizer _directoryOrganizer;
        private readonly string _tempDirectory;

        public DirectoryOrganizerTests()
        {
            _directoryOrganizer = new DirectoryOrganizer();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [Theory]
        [InlineData(1, "enero")]
        [InlineData(2, "febrero")]
        [InlineData(3, "marzo")]
        [InlineData(4, "abril")]
        [InlineData(5, "mayo")]
        [InlineData(6, "junio")]
        [InlineData(7, "julio")]
        [InlineData(8, "agosto")]
        [InlineData(9, "septiembre")]
        [InlineData(10, "octubre")]
        [InlineData(11, "noviembre")]
        [InlineData(12, "diciembre")]
        public void GetMonthName_WithValidMonth_ShouldReturnSpanishName(int monthNumber, string expected)
        {
            // Act
            var result = _directoryOrganizer.GetMonthName(monthNumber);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        public void GetMonthName_WithInvalidMonth_ShouldReturnUnknown(int monthNumber)
        {
            // Act
            var result = _directoryOrganizer.GetMonthName(monthNumber);

            // Assert
            result.Should().Be("unknown");
        }

        [Fact]
        public void IsInYearMonthStructure_WithMonthInYearFolder_ShouldReturnTrue()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            var monthPath = Path.Combine(yearPath, "03_marzo");
            Directory.CreateDirectory(monthPath);

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(monthPath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsInYearMonthStructure_WithYearFolder_ShouldReturnTrue()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            Directory.CreateDirectory(yearPath);

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(yearPath);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("regular_folder")]
        [InlineData("2024_not_year")]
        [InlineData("03_marzo_not_in_year")]
        public void IsInYearMonthStructure_WithInvalidStructure_ShouldReturnFalse(string folderName)
        {
            // Arrange
            var folderPath = Path.Combine(_tempDirectory, folderName);
            Directory.CreateDirectory(folderPath);

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(folderPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsInYearMonthStructure_WithNonExistentPath_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "non_existent");

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(nonExistentPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithValidDate_ShouldCreateStructureAndMoveFile()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-03-15";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            var expectedPath = Path.Combine(_tempDirectory, "2024", "03_marzo", "test.pdf");
            result.Should().Be(expectedPath);
            File.Exists(expectedPath).Should().BeTrue();
            File.Exists(testFile).Should().BeFalse();
            Directory.Exists(Path.Combine(_tempDirectory, "2024")).Should().BeTrue();
            Directory.Exists(Path.Combine(_tempDirectory, "2024", "03_marzo")).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithInvalidDate_ShouldReturnOriginalPath()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var invalidDateStr = "invalid-date";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, invalidDateStr);

            // Assert
            result.Should().Be(testFile);
            File.Exists(testFile).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithEmptyDate_ShouldReturnOriginalPath()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, string.Empty);

            // Assert
            result.Should().Be(testFile);
            File.Exists(testFile).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_AlreadyInCorrectStructure_ShouldReturnSamePath()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            var monthPath = Path.Combine(yearPath, "03_marzo");
            Directory.CreateDirectory(monthPath);
            
            var testFile = Path.Combine(monthPath, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-03-15";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            result.Should().Be(testFile);
            File.Exists(testFile).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_InYearFolderDifferentMonth_ShouldMoveToCorrectMonth()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            var wrongMonthPath = Path.Combine(yearPath, "02_febrero");
            Directory.CreateDirectory(wrongMonthPath);
            
            var testFile = Path.Combine(wrongMonthPath, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-03-15"; // March date

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            var expectedPath = Path.Combine(yearPath, "03_marzo", "test.pdf");
            result.Should().Be(expectedPath);
            File.Exists(expectedPath).Should().BeTrue();
            File.Exists(testFile).Should().BeFalse();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_AlreadyInYearFolder_ShouldMoveToMonthFolder()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            Directory.CreateDirectory(yearPath);
            
            var testFile = Path.Combine(yearPath, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-03-15";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            // When already in year folder, the base path goes back to temp directory level
            var expectedPath = Path.Combine(_tempDirectory, "2024", "03_marzo", "test.pdf");
            result.Should().Be(expectedPath);
            File.Exists(result).Should().BeTrue();
            File.Exists(testFile).Should().BeFalse();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithDuplicateFile_ShouldCreateUniqueFilename()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            var monthPath = Path.Combine(yearPath, "03_marzo");
            Directory.CreateDirectory(monthPath);
            
            var existingFile = Path.Combine(monthPath, "test.pdf");
            File.WriteAllText(existingFile, "existing content");
            
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-03-15";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            var expectedPath = Path.Combine(monthPath, "test_2.pdf");
            result.Should().Be(expectedPath);
            File.Exists(expectedPath).Should().BeTrue();
            File.Exists(existingFile).Should().BeTrue();
            File.Exists(testFile).Should().BeFalse();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithMultipleDuplicates_ShouldCreateUniqueFilename()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            var monthPath = Path.Combine(yearPath, "03_marzo");
            Directory.CreateDirectory(monthPath);
            
            File.WriteAllText(Path.Combine(monthPath, "test.pdf"), "existing 1");
            File.WriteAllText(Path.Combine(monthPath, "test_2.pdf"), "existing 2");
            File.WriteAllText(Path.Combine(monthPath, "test_3.pdf"), "existing 3");
            
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-03-15";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            var expectedPath = Path.Combine(monthPath, "test_4.pdf");
            result.Should().Be(expectedPath);
            File.Exists(expectedPath).Should().BeTrue();
            File.Exists(testFile).Should().BeFalse();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithDifferentYears_ShouldCreateNewYearStructure()
        {
            // Arrange
            var existingYearPath = Path.Combine(_tempDirectory, "2023");
            Directory.CreateDirectory(existingYearPath);
            
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-03-15";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            var expectedPath = Path.Combine(_tempDirectory, "2024", "03_marzo", "test.pdf");
            result.Should().Be(expectedPath);
            File.Exists(expectedPath).Should().BeTrue();
            File.Exists(testFile).Should().BeFalse();
            Directory.Exists(Path.Combine(_tempDirectory, "2024")).Should().BeTrue();
            Directory.Exists(Path.Combine(_tempDirectory, "2024", "03_marzo")).Should().BeTrue();
        }

        [Fact]
        public async Task OrganizeFileIntoDirectoryStructure_ThreadSafety_ShouldHandleConcurrentCalls()
        {
            // Arrange
            var tasks = new List<Task<string>>();
            var dateStr = "2024-03-15";

            // Act - simulate concurrent file organization
            for (int i = 0; i < 5; i++)
            {
                int fileNumber = i;
                tasks.Add(Task.Run(() =>
                {
                    var testFile = Path.Combine(_tempDirectory, $"test_{fileNumber}.pdf");
                    File.WriteAllText(testFile, $"test content {fileNumber}");
                    return _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);
                }));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(5);
            results.Should().OnlyHaveUniqueItems();
            
            foreach (var result in results)
            {
                File.Exists(result).Should().BeTrue();
                result.Should().Contain(Path.Combine("2024", "03_marzo"));
            }
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
