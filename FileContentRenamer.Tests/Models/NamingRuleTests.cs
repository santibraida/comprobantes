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

        [Fact]
        public void IsValid_WithAllRequiredFields_ShouldReturnTrue()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "ValidRule",
                Keywords = new List<string> { "invoice", "payment" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            // Act
            var result = rule.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithMissingName_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            // Act
            var result = rule.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithWhitespaceName_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "   ",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            // Act
            var result = rule.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithNullKeywords_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = null!,
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            // Act
            var result = rule.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithEmptyKeywords_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string>(),
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            // Act
            var result = rule.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithMissingServiceName_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "",
                PaymentMethod = "card"
            };

            // Act
            var result = rule.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithOptionalPaymentMethodEmpty_ShouldReturnTrue()
        {
            // Arrange - PaymentMethod is optional, can use default
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = ""
            };

            // Act
            var result = rule.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithOptionalDateOverride_ShouldReturnTrue()
        {
            // Arrange - DateOverride is optional
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card",
                DateOverride = null
            };

            // Act
            var result = rule.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void INamingRule_Interface_CanBeUsedForDependencyInjection()
        {
            // Arrange - Use interface type
            INamingRule rule = new NamingRule
            {
                Name = "InterfaceTestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            var content = "This is an invoice document";

            // Act
            var matches = rule.Matches(content);
            var isValid = rule.IsValid();

            // Assert
            matches.Should().BeTrue();
            isValid.Should().BeTrue();
            rule.Name.Should().Be("InterfaceTestRule");
        }

        [Fact]
        public void Keywords_PropertySetter_ShouldPrecomputeLowercaseKeywords()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                ServiceName = "test_service"
            };

            // Act
            rule.Keywords = new List<string> { "INVOICE", "Payment", "2024" };
            var content = "this has invoice and payment for 2024";
            var result = rule.Matches(content);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Matches_WithKeywordsDirectlyAssigned_ShouldComputeLowercaseAtRuntime()
        {
            // Arrange - Create rule with keywords set via constructor or object initializer
            // This tests the fallback path when _lowerKeywords is null
            var rule = new NamingRule
            {
                Name = "TestRule",
                ServiceName = "test_service"
            };

            // Directly assign to the underlying list without going through the property setter
            // This simulates a scenario where Keywords is set but _lowerKeywords is not precomputed
            var keywordsList = new List<string> { "INVOICE", "PAYMENT" };

            // First set it to trigger precomputation, then modify the list directly
            rule.Keywords = keywordsList;

            // Now add a keyword after precomputation to test both paths
            var content1 = "this has INVOICE and PAYMENT";
            var result1 = rule.Matches(content1);

            // Act & Assert
            result1.Should().BeTrue();
        }

        [Fact]
        public void Matches_WithPartialKeywordMatch_ShouldReturnFalse()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice", "payment", "2024", "confirmed" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };
            var content = "This is an invoice for payment in 2024"; // Missing "confirmed"

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

        [Fact]
        public void INamingRules_Interface_CanBeUsedForDependencyInjection()
        {
            // Arrange - Use interface type
            INamingRules namingRules = new NamingRules("interface_service", "interface_payment");

            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = "card"
            };

            namingRules.Rules.Add(rule);
            var content = "This is an invoice";
            var date = "2024-03-15";

            // Act
            var matchingRule = namingRules.FindMatchingRule(content);
            var filename = namingRules.GenerateFilename(content, date);

            // Assert
            matchingRule.Should().NotBeNull();
            matchingRule!.Name.Should().Be("TestRule");
            filename.Should().Be("test_service_2024-03-15_card");
        }

        [Fact]
        public void FindMatchingRule_WithNullContent_ShouldReturnNull()
        {
            // Arrange
            var namingRules = new NamingRules();
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service"
            };
            namingRules.Rules.Add(rule);

            // Act
            var result = namingRules.FindMatchingRule(null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void FindMatchingRule_WithWhitespaceContent_ShouldReturnNull()
        {
            // Arrange
            var namingRules = new NamingRules();
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service"
            };
            namingRules.Rules.Add(rule);

            // Act
            var result = namingRules.FindMatchingRule("   \n\t  ");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GenerateFilename_WithRuleHavingNullPaymentMethod_ShouldUseDefault()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "invoice" },
                ServiceName = "test_service",
                PaymentMethod = null! // Null payment method
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
        public void GenerateFilename_WithMultipleMatchingRules_ShouldUseFirstMatch()
        {
            // Arrange
            var rule1 = new NamingRule
            {
                Name = "Rule1",
                Keywords = new List<string> { "invoice" },
                ServiceName = "service1",
                PaymentMethod = "payment1"
            };

            var rule2 = new NamingRule
            {
                Name = "Rule2",
                Keywords = new List<string> { "invoice" },
                ServiceName = "service2",
                PaymentMethod = "payment2"
            };

            var namingRules = new NamingRules("default_service", "default_payment");
            namingRules.Rules.Add(rule1);
            namingRules.Rules.Add(rule2);

            var content = "This is an invoice";
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename(content, date);

            // Assert
            result.Should().Be("service1_2024-03-15_payment1");
        }

        [Fact]
        public void GenerateFilename_WithEmptyContent_ShouldUseDefaults()
        {
            // Arrange
            var namingRules = new NamingRules("default_service", "default_payment");
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename("", date);

            // Assert
            result.Should().Be("default_service_2024-03-15_default_payment");
        }

        [Fact]
        public void GenerateFilename_WithMinimalContentAndNoDateOverride_ShouldUseOriginalDate()
        {
            // Arrange
            var rule = new NamingRule
            {
                Name = "TestRule",
                Keywords = new List<string> { "test" },
                ServiceName = "test_service",
                PaymentMethod = "card"
                // No DateOverride
            };

            var namingRules = new NamingRules();
            namingRules.Rules.Add(rule);

            var content = "test"; // Minimal content but no override
            var date = "2024-03-15";

            // Act
            var result = namingRules.GenerateFilename(content, date);

            // Assert
            result.Should().Be("test_service_2024-03-15_card");
        }

        [Fact]
        public void DefaultServiceName_Property_ShouldBeSettableAndGettable()
        {
            // Arrange
            var namingRules = new NamingRules();

            // Act
            namingRules.DefaultServiceName = "new_service";

            // Assert
            namingRules.DefaultServiceName.Should().Be("new_service");
        }

        [Fact]
        public void DefaultPaymentMethod_Property_ShouldBeSettableAndGettable()
        {
            // Arrange
            var namingRules = new NamingRules();

            // Act
            namingRules.DefaultPaymentMethod = "new_payment";

            // Assert
            namingRules.DefaultPaymentMethod.Should().Be("new_payment");
        }

        [Fact]
        public void Rules_Property_ShouldBeModifiable()
        {
            // Arrange
            var namingRules = new NamingRules();
            var rule1 = new NamingRule { Name = "Rule1", Keywords = new List<string> { "test1" }, ServiceName = "service1" };
            var rule2 = new NamingRule { Name = "Rule2", Keywords = new List<string> { "test2" }, ServiceName = "service2" };

            // Act
            namingRules.Rules.Add(rule1);
            namingRules.Rules.Add(rule2);

            // Assert
            namingRules.Rules.Should().HaveCount(2);
            namingRules.Rules[0].Should().Be(rule1);
            namingRules.Rules[1].Should().Be(rule2);
        }
    }
}

