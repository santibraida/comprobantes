using FileContentRenamer.Services;
using FluentAssertions;
using Xunit;

namespace FileContentRenamer.Tests.Services
{
    public class DateExtractorTests
    {
        private readonly DateExtractor _dateExtractor;

        public DateExtractorTests()
        {
            _dateExtractor = new DateExtractor();
        }

        [Fact]
        public void ExtractDateFromContent_WithSpanishDateFormat_ShouldReturnCorrectDate()
        {
            // Arrange
            var content = "Fecha: 15 de marzo de 2024";

            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be("2024-03-15");
        }

        [Theory]
        [InlineData("1 de enero de 2024", "2024-01-01")]
        [InlineData("31 de diciembre de 2023", "2023-12-31")]
        [InlineData("15 de junio de 2025", "2025-06-15")]
        [InlineData("Vto.:15/03/2024", "2024-03-15")]
        [InlineData("vencimiento 01/01/2025", "2025-01-01")]
        [InlineData("Vence: 31/12/2023", "2023-12-31")]
        [InlineData("15/03/2024", "2024-03-15")]
        [InlineData("01/01/2025", "2025-01-01")]
        [InlineData("31/12/2023", "2023-12-31")]
        public void ExtractDateFromContent_WithVariousDateFormats_ShouldReturnCorrectDates(string content, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractDateFromContent_WithEmissionDateFormat_ShouldReturnCorrectDate()
        {
            // Arrange
            var content = "EMISIÓN: 15/03/2024";

            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be("2024-03-15");
        }

        [Fact]
        public void ExtractDateFromContent_WithNoDate_ShouldReturnEmptyString()
        {
            // Arrange
            var content = "This is content without any date";

            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractDateFromContent_WithNullContent_ShouldReturnEmptyString()
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(null!);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractDateFromContent_WithEmptyContent_ShouldReturnEmptyString()
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(string.Empty);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractDateFromFilename_WithValidFormat_ShouldReturnCorrectDate()
        {
            // Arrange
            var filename = "service_2024-03-15_payment";

            // Act
            var result = _dateExtractor.ExtractDateFromFilename(filename);

            // Assert
            result.Should().Be("2024-03-15");
        }

        [Theory]
        [InlineData("service_2024-01-01_payment", "2024-01-01")]
        [InlineData("test_2023-12-31_method", "2023-12-31")]
        [InlineData("invoice_2025-06-15_bank", "2025-06-15")]
        public void ExtractDateFromFilename_WithVariousValidFormats_ShouldReturnCorrectDates(string filename, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromFilename(filename);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractDateFromFilename_WithInvalidFormat_ShouldReturnEmptyString()
        {
            // Arrange
            var filename = "service_payment_nodate";

            // Act
            var result = _dateExtractor.ExtractDateFromFilename(filename);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractDateFromFilename_WithNullFilename_ShouldReturnEmptyString()
        {
            // Act
            var result = _dateExtractor.ExtractDateFromFilename(null!);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractDateFromFilename_WithEmptyFilename_ShouldReturnEmptyString()
        {
            // Act
            var result = _dateExtractor.ExtractDateFromFilename(string.Empty);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractDateFromContent_WithMultipleDates_ShouldReturnFirstFound()
        {
            // Arrange
            var testContent = "15 de marzo de 2024 and also 16 de abril de 2024";

            // Act
            var testResult = _dateExtractor.ExtractDateFromContent(testContent);

            // Assert
            testResult.Should().Be("2024-03-15");
        }

        [Theory]
        [InlineData("service_2024-01-01_payment", "2024-01-01")]
        [InlineData("test_2023-12-31_method", "2023-12-31")]
        [InlineData("invoice_2025-06-15_bank", "2025-06-15")]
        [InlineData("service_payment_nodate", "")]
        public void ExtractDateFromFilename_WithVariousFormats_ShouldReturnExpectedResult(string filename, string expected)
        {
            // Act
            var testResult = _dateExtractor.ExtractDateFromFilename(filename);

            // Assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ExtractDateFromContent_WithPartialInvalidDate_ShouldFindValidDate()
        {
            // Arrange
            var testContent = "15 de invalidmonth de 2024 but also 16/03/2024";

            // Act
            var testResult = _dateExtractor.ExtractDateFromContent(testContent);

            // Assert
            testResult.Should().Be("2024-03-16");
        }

        [Theory]
        [InlineData("EMISIÓN: 15/03/2024", "2024-03-15")]
        [InlineData("Fecha 01/01/2025", "2025-01-01")]
        [InlineData("FECHA: 31/12/2023", "2023-12-31")]
        [InlineData("emisión 20/06/2024", "2024-06-20")]
        public void ExtractDateFromContent_WithAdditionalEmissionPatterns_ShouldReturnCorrectDate(string content, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("vencimiento 15/03/2024", "2024-03-15")]
        [InlineData("Vto. 01/01/2025", "2025-01-01")]
        [InlineData("Vto.: 31/12/2023", "2023-12-31")]
        [InlineData("vencimiento: 20/06/2024", "2024-06-20")]
        public void ExtractDateFromContent_WithAdditionalDueDatePatterns_ShouldReturnCorrectDate(string content, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("15-03-2024", "2024-03-15")]
        [InlineData("01-01-2025", "2025-01-01")]
        [InlineData("2024-03-15", "2024-03-15")]
        [InlineData("2025-12-31", "2025-12-31")]
        public void ExtractDateFromContent_WithHyphenSeparators_ShouldReturnCorrectDate(string content, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("1 de febrero de 2024", "2024-02-01")]
        [InlineData("5 de mayo de 2024", "2024-05-05")]
        [InlineData("9 de septiembre de 2024", "2024-09-09")]
        public void ExtractDateFromContent_WithSingleDigitSpanishDays_ShouldPadCorrectly(string content, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("15 de abril de 2024")]
        [InlineData("30 de julio de 2024")]
        [InlineData("20 de agosto de 2024")]
        [InlineData("10 de octubre de 2024")]
        [InlineData("25 de noviembre de 2024")]
        public void ExtractDateFromContent_WithAllSpanishMonths_ShouldExtractCorrectly(string content)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain("2024");
        }

        [Fact]
        public void ExtractDateFromContent_WithWhitespaceOnly_ShouldReturnEmptyString()
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent("   ");

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("5/3/2024", "2024-03-05")]
        [InlineData("1/1/2025", "2025-01-01")]
        [InlineData("9/12/2023", "2023-12-09")]
        public void ExtractDateFromContent_WithSingleDigitMonthAndDay_ShouldPadCorrectly(string content, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be(expected);
        }
    }
}

