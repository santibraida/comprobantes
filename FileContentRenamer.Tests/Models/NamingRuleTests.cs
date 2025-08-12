using FileContentRenamer.Models;
using FluentAssertions;
using Xunit;

namespace FileContentRenamer.Tests.Models
{
    public class NamingRuleTests
    {
        [Fact]
        public void Matches_WithAllKeywordsPresent_ShouldReturnTrue()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice", "payment", "2024" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            var content = "This is an invoice for payment in 2024";

            // Act
            var result = rule.Matches(content);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Matches_WithMissingKeywords_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice", "payment", "2024" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            var content = "This is an invoice for 2023"; // Missing "payment" and has wrong year

            // Act
            var result = rule.Matches(content);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Matches_WithCaseInsensitiveKeywords_ShouldReturnTrue()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "INVOICE", "payment", "2024" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            var content = "this is an invoice for Payment in 2024";

            // Act
            var result = rule.Matches(content);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Matches_WithEmptyContent_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            // Act
            var result = rule.Matches(string.Empty);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Matches_WithNullContent_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            // Act
            var result = rule.Matches(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Matches_WithNoKeywords_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string>(),
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            var content = "This is some content";

            // Act
            var result = rule.Matches(content);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Matches_WithNullKeywords_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = null!,
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            var content = "This is some content";

            // Act
            var result = rule.Matches(content);

            // Assert
            result.Should().BeFalse();
        }
    }

    public class NamingRulesTests
    {
        [Fact]
        public void Constructor_WithDefaultValues_ShouldInitializeCorrectly()
        {
            // Act
            var namingRules = new NamingRules();

            // Assert
            namingRules.Rules.Should().NotBeNull();
            namingRules.Rules.Should().BeEmpty();
            namingRules.DefaultServiceName.Should().Be("");
            namingRules.DefaultPaymentMethod.Should().Be("");
        }

        [Fact]
        public void Constructor_WithParameters_ShouldSetValues()
        {
            // Act
            var namingRules = new NamingRules("test_service", "test_payment");

            // Assert
            namingRules.DefaultServiceName.Should().Be("test_service");
            namingRules.DefaultPaymentMethod.Should().Be("test_payment");
        }

        [Fact]
        public void FindMatchingRule_WithMatchingRule_ShouldReturnRule()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice", "payment" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            
            var namingRules = new NamingRules();
            namingRules.Rules.Add(rule);
            
            var content = "This is an invoice for payment";

            // Act
            var result = namingRules.FindMatchingRule(content);

            // Assert
            result.Should().Be(rule);
        }

        [Fact]
        public void FindMatchingRule_WithNoMatchingRule_ShouldReturnNull()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice", "payment" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            
            var namingRules = new NamingRules();
            namingRules.Rules.Add(rule);
            
            var content = "This is just some random content";

            // Act
            var result = namingRules.FindMatchingRule(content);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void FindMatchingRule_WithEmptyContent_ShouldReturnNull()
        {
            // Arrange
            var namingRules = new NamingRules();
            
            // Act
            var result = namingRules.FindMatchingRule(string.Empty);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void FindMatchingRule_WithFirstRuleMatching_ShouldReturnFirstRule()
        {
            // Arrange
            var rule1 = new NamingRule
            {
                Name = "Rule1",
                Keywords = new List<string> { "invoice" },
                ServiceName = "service1",
                PaymentMethod = "card1"
            };
            
            var rule2 = new NamingRule
            {
                Name = "Rule2",
                Keywords = new List<string> { "invoice" },
                ServiceName = "service2",
                PaymentMethod = "card2"
            };
            
            var namingRules = new NamingRules();
            namingRules.Rules.Add(rule1);
            namingRules.Rules.Add(rule2);
            
            var content = "This is an invoice";

            // Act
            var result = namingRules.FindMatchingRule(content);

            // Assert
            result.Should().Be(rule1);
        }

        [Fact]
        public void GenerateFilename_WithMatchingRule_ShouldUseRuleValues()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            
            var namingRules = new NamingRules("default_service", "default_payment");
            namingRules.Rules.Add(rule);
            
            var content = "This is an invoice";
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename(content, date);

            // Assert
            result.Should().Be("test_service_2024-03-15_card");
        }

        [Fact]
        public void GenerateFilename_WithNoMatchingRule_ShouldUseDefaults()
        {
            // Arrange
            var namingRules = new NamingRules("default_service", "default_payment");
            var content = "Random content with no matching keywords";
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename(content, date);

            // Assert
            result.Should().Be("default_service_2024-03-15_default_payment");
        }

        [Fact]
        public void GenerateFilename_WithRuleWithEmptyPaymentMethod_ShouldUseDefault()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "" // Empty payment method
            };
            
            var namingRules = new NamingRules("default_service", "default_payment");
            namingRules.Rules.Add(rule);
            
            var content = "This is an invoice";
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename(content, date);

            // Assert
            result.Should().Be("test_service_2024-03-15_default_payment");
        }

        [Fact]
        public void GenerateFilename_WithDateOverrideAndMinimalContent_ShouldUseDateOverride()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "mercado", "gloria" },
                ServiceName = "test_service",
                PaymentMethod = "transfer",
                DateOverride = "2024-01-15"
            };
            
            var namingRules = new NamingRules();
            namingRules.Rules.Add(rule);
            
            var content = "mercado gloria"; // Minimal content without date
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename(content, date);

            // Assert
            result.Should().Be("test_service_2024-01-15_transfer");
        }

        [Fact]
        public void GenerateFilename_WithDateOverrideButRichContent_ShouldUseOriginalDate()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "mercado", "gloria" },
                ServiceName = "test_service",
                PaymentMethod = "transfer",
                DateOverride = "2024-01-15"
            };
            
            var namingRules = new NamingRules();
            namingRules.Rules.Add(rule);
            
            var content = "mercado gloria transfer payment with date 15/03/2024 and more details"; // Rich content
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename(content, date);

            // Assert
            result.Should().Be("test_service_2024-03-15_transfer");
        }

        [Fact]
        public void GenerateFilename_WithMinimalGenericContent_ShouldUseOriginalDate()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card",
                DateOverride = "2024-01-15"
            };
            
            var namingRules = new NamingRules();
            namingRules.Rules.Add(rule);
            
            var content = "invoice"; // Minimal content, should use DateOverride
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename(content, date);

            // Assert - should use DateOverride because content is minimal
            result.Should().Be("test_service_2024-01-15_card");
        }
    }
}
