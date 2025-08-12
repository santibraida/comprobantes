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
        public void ExtractDateFromContent_WithVariousSpanishDates_ShouldReturnCorrectDates(string content, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractDateFromContent_WithDueDateFormat_ShouldReturnCorrectDate()
        {
            // Arrange
            var content = "vencimiento 15/03/2024";

            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be("2024-03-15");
        }

        [Theory]
        [InlineData("Vto.:15/03/2024", "2024-03-15")]
        [InlineData("vencimiento 01/01/2025", "2025-01-01")]
        [InlineData("Vence: 31/12/2023", "2023-12-31")]
        public void ExtractDateFromContent_WithVariousDueDateFormats_ShouldReturnCorrectDates(string content, string expected)
        {
            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractDateFromContent_WithGenericDateFormat_ShouldReturnCorrectDate()
        {
            // Arrange
            var content = "Fecha: 15/03/2024";

            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be("2024-03-15");
        }

        [Theory]
        [InlineData("15/03/2024", "2024-03-15")]
        [InlineData("01/01/2025", "2025-01-01")]
        [InlineData("31/12/2023", "2023-12-31")]
        public void ExtractDateFromContent_WithVariousGenericDates_ShouldReturnCorrectDates(string content, string expected)
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
        public void ExtractDateFromFilename_WithNullFilename_ShouldThrowArgumentNullException()
        {
            // Act & Assert - The actual implementation throws when null is passed to Regex.Match
            Action act = () => _dateExtractor.ExtractDateFromFilename(null!);
            act.Should().Throw<ArgumentNullException>();
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
            var content = "vencimiento 15/03/2024 and also 20/05/2024";

            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be("2024-03-15");
        }

        [Fact]
        public void ExtractDateFromContent_WithInvalidSpanishMonth_ShouldFallbackToOtherFormats()
        {
            // Arrange
            var content = "15 de invalidmonth de 2024 but also 16/03/2024";

            // Act
            var result = _dateExtractor.ExtractDateFromContent(content);

            // Assert
            result.Should().Be("2024-03-16");
        }
    }
}
