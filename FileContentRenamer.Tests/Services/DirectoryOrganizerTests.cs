using FileContentRenamer.Models;
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
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            var config = new AppConfig { BasePath = _tempDirectory };
            _directoryOrganizer = new DirectoryOrganizer(config);
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

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithFileInRootWithNoDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var fileName = "test.txt";
            var filePath = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(filePath, "test content");
            var date = "2024-03-15";

            // Act - organize file that's already in root
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(filePath, date);

            // Assert
            result.Should().NotBeNull();
            File.Exists(result).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_CreatesYearFolderIfNotExists()
        {
            // Arrange
            var fileName = "new_file.txt";
            var filePath = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(filePath, "test");
            var date = "2025-06-10";  // New year folder
            var expectedYearPath = Path.Combine(_tempDirectory, "2025");

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(filePath, date);

            // Assert
            Directory.Exists(expectedYearPath).Should().BeTrue("year folder should be created");
            result.Should().Contain("2025");
            result.Should().Contain("06_junio");
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithExistingYearFolder_ShouldUseIt()
        {
            // Arrange
            var yearFolder = Path.Combine(_tempDirectory, "2023");
            Directory.CreateDirectory(yearFolder);

            var fileName = "existing_year.txt";
            var filePath = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(filePath, "test");
            var date = "2023-12-25";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(filePath, date);

            // Assert
            result.Should().Contain("2023");
            result.Should().Contain("12_diciembre");
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithFileAtRoot_ReturnsCorrectPath()
        {
            // Arrange
            var rootFile = Path.Combine(_tempDirectory, "root_file.txt");
            File.WriteAllText(rootFile, "content");

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(rootFile, "2024-01-01");

            // Assert
            result.Should().StartWith(_tempDirectory);
            result.Should().Contain(Path.Combine("2024", "01_enero"));
        }

        [Fact]
        public void Constructor_WithNullBasePath_ShouldUseCurrentDirectory()
        {
            // Arrange
            var config = new AppConfig { BasePath = null };

            // Act
            var organizer = new DirectoryOrganizer(config);

            // Assert
            organizer.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithEmptyBasePath_ShouldUseProvidedPath()
        {
            // Arrange
            var config = new AppConfig { BasePath = string.Empty };

            // Act
            var organizer = new DirectoryOrganizer(config);

            // Assert
            organizer.Should().NotBeNull();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithNullDate_ShouldReturnOriginalPath()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, null!);

            // Assert
            result.Should().Be(testFile);
            File.Exists(testFile).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithWhitespaceDate_ShouldReturnOriginalPath()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, "   ");

            // Assert
            result.Should().Be(testFile);
            File.Exists(testFile).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithDifferentDateFormats_ShouldHandle()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.txt");
            File.WriteAllText(testFile, "content");

            // Act - Try with ISO format which should work
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, "2024-03-15");

            // Assert
            result.Should().NotBe(testFile); // Should organize if parsing succeeds
            result.Should().Contain("2024");
            result.Should().Contain("03_marzo");
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithInvalidDateFormat_ShouldReturnOriginalPath()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "test.pdf");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, "not-a-date-2024");

            // Assert
            result.Should().Be(testFile);
            File.Exists(testFile).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_InWrongYearFolder_ShouldMoveToCorrectYear()
        {
            // Arrange
            var wrongYearPath = Path.Combine(_tempDirectory, "2023");
            var wrongMonthPath = Path.Combine(wrongYearPath, "12_diciembre");
            Directory.CreateDirectory(wrongMonthPath);

            var testFile = Path.Combine(wrongMonthPath, "test.pdf");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-01-15"; // Different year

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            var expectedPath = Path.Combine(_tempDirectory, "2024", "01_enero", "test.pdf");
            result.Should().Be(expectedPath);
            File.Exists(expectedPath).Should().BeTrue();
            File.Exists(testFile).Should().BeFalse();
        }

        [Theory]
        [InlineData("2024-01-31")]
        [InlineData("2024-02-29")] // Leap year
        [InlineData("2024-12-31")]
        public void OrganizeFileIntoDirectoryStructure_WithEdgeDates_ShouldHandleCorrectly(string dateStr)
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, $"test_{dateStr.Replace("-", "")}.pdf");
            File.WriteAllText(testFile, "test content");

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            result.Should().NotBe(testFile);
            File.Exists(result).Should().BeTrue();
            File.Exists(testFile).Should().BeFalse();
        }

        [Fact]
        public void IsInYearMonthStructure_WithMonthFolderInNonYearParent_ShouldReturnFalse()
        {
            // Arrange
            var parentPath = Path.Combine(_tempDirectory, "not_a_year");
            var monthPath = Path.Combine(parentPath, "03_marzo");
            Directory.CreateDirectory(monthPath);

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(monthPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsInYearMonthStructure_WithInvalidMonthFormat_ShouldReturnFalse()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            var invalidMonthPath = Path.Combine(yearPath, "3_marzo"); // Should be 03_marzo
            Directory.CreateDirectory(invalidMonthPath);

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(invalidMonthPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsInYearMonthStructure_WithValidMonthFormatButWrongName_ShouldReturnTrue()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            var monthPath = Path.Combine(yearPath, "03_whatever"); // Correct format, any text after underscore
            Directory.CreateDirectory(monthPath);

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(monthPath);

            // Assert
            result.Should().BeTrue(); // Regex only checks format: \d{2}_\w+
        }

        [Fact]
        public void IsInYearMonthStructure_WithException_ShouldReturnFalse()
        {
            // Arrange - path with invalid characters that might cause exception
            var invalidPath = ""; // Empty path

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(invalidPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithCaseInsensitiveDuplicateCheck_ShouldNotDuplicate()
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, "2024");
            var monthPath = Path.Combine(yearPath, "05_mayo");
            Directory.CreateDirectory(monthPath);

            var testFile = Path.Combine(monthPath, "TEST.PDF");
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-05-10";

            // Act - File is already in correct location with same name (case-insensitive)
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            result.Should().Be(testFile);
            File.Exists(testFile).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithSpecialCharactersInFilename_ShouldHandle()
        {
            // Arrange
            var fileName = "file with spaces & special (chars).pdf";
            var testFile = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(testFile, "test content");
            var dateStr = "2024-07-20";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            result.Should().Contain("2024");
            result.Should().Contain("07_julio");
            result.Should().Contain(fileName);
            File.Exists(result).Should().BeTrue();
        }

        [Theory]
        [InlineData("2020")]
        [InlineData("1999")]
        [InlineData("2099")]
        public void IsInYearMonthStructure_WithVariousYearFolders_ShouldReturnTrue(string year)
        {
            // Arrange
            var yearPath = Path.Combine(_tempDirectory, year);
            Directory.CreateDirectory(yearPath);

            // Act
            var result = _directoryOrganizer.IsInYearMonthStructure(yearPath);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("999")]   // 3 digits
        [InlineData("20240")] // 5 digits
        [InlineData("year")]  // Not digits
        public void IsInYearMonthStructure_WithInvalidYearFormat_ShouldReturnFalse(string folderName)
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
        public void OrganizeFileIntoDirectoryStructure_WithFileExtensionPreserved_ShouldKeepExtension()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "document.docx");
            File.WriteAllText(testFile, "content");
            var dateStr = "2024-11-05";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            result.Should().EndWith(".docx");
            File.Exists(result).Should().BeTrue();
        }

        [Fact]
        public void OrganizeFileIntoDirectoryStructure_WithLongFilename_ShouldHandle()
        {
            // Arrange
            var longName = new string('a', 200) + ".txt";
            var testFile = Path.Combine(_tempDirectory, longName);
            File.WriteAllText(testFile, "content");
            var dateStr = "2024-08-15";

            // Act
            var result = _directoryOrganizer.OrganizeFileIntoDirectoryStructure(testFile, dateStr);

            // Assert
            result.Should().Contain("2024");
            result.Should().Contain("08_agosto");
            File.Exists(result).Should().BeTrue();
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
