using AegisEInvoicing.Persistence;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("PascalCase", "pascal_case")]
    [InlineData("camelCase", "camel_case")]
    [InlineData("XMLHttpRequest", "x_m_l_http_request")]
    [InlineData("HTTPSConnection", "h_t_t_p_s_connection")]
    [InlineData("UserID", "user_i_d")]
    [InlineData("BusinessName", "business_name")]
    [InlineData("InvoiceTransmissionQueue", "invoice_transmission_queue")]
    [InlineData("FIRSApiConfiguration", "f_i_r_s_api_configuration")]
    [InlineData("OAuth2Token", "o_auth2_token")]
    public void ToSnakeCase_WithPascalAndCamelCase_ShouldConvertToSnakeCase(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("lowercase", "lowercase")]
    [InlineData("UPPERCASE", "u_p_p_e_r_c_a_s_e")]
    [InlineData("MixedCASE", "mixed_c_a_s_e")]
    [InlineData("already_snake_case", "already_snake_case")]
    [InlineData("mixed_With_CASE", "mixed__with__c_a_s_e")]
    public void ToSnakeCase_WithVariousCasing_ShouldHandleCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("A", "a")]
    [InlineData("B", "b")]
    [InlineData("Z", "z")]
    public void ToSnakeCase_WithSingleCharacter_ShouldConvertToLowercase(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("AB", "a_b")]
    [InlineData("Aa", "aa")]
    [InlineData("AA", "a_a")]
    [InlineData("aA", "a_a")]
    public void ToSnakeCase_WithTwoCharacters_ShouldHandleCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToSnakeCase_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ToSnakeCase_WithNullString_ShouldReturnNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input!.ToSnakeCase();

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("123", "123")]
    [InlineData("Test123", "test123")]
    [InlineData("123Test", "123_test")]
    [InlineData("Test123Test", "test123_test")]
    public void ToSnakeCase_WithNumbers_ShouldHandleCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Test-Case", "test-_case")]
    [InlineData("Test_Case", "test__case")]
    [InlineData("Test Case", "test _case")]
    [InlineData("Test.Case", "test._case")]
    [InlineData("Test@Case", "test@_case")]
    public void ToSnakeCase_WithSpecialCharacters_ShouldPreserveSpecialCharacters(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("API", "a_p_i")]
    [InlineData("URL", "u_r_l")]
    [InlineData("HTML", "h_t_m_l")]
    [InlineData("JSON", "j_s_o_n")]
    [InlineData("XML", "x_m_l")]
    public void ToSnakeCase_WithCommonAcronyms_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("GetUserByID", "get_user_by_i_d")]
    [InlineData("SetHTTPSConnection", "set_h_t_t_p_s_connection")]
    [InlineData("ParseXMLDocument", "parse_x_m_l_document")]
    [InlineData("CreateJSONResponse", "create_j_s_o_n_response")]
    public void ToSnakeCase_WithMethodNames_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("BusinessFIRSApiConfiguration", "business_f_i_r_s_api_configuration")]
    [InlineData("InvoiceTransmissionQueue", "invoice_transmission_queue")]
    [InlineData("SystemConfiguration", "system_configuration")]
    [InlineData("ApiUsageTracking", "api_usage_tracking")]
    [InlineData("UserRoleAssignment", "user_role_assignment")]
    public void ToSnakeCase_WithEntityNames_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("IOError", "i_o_error")]
    [InlineData("HTTPSConnection", "h_t_t_p_s_connection")]
    [InlineData("XMLHttpRequest", "x_m_l_http_request")]
    [InlineData("HTTPSProxy", "h_t_t_p_s_proxy")]
    public void ToSnakeCase_WithConsecutiveUppercaseLetters_ShouldHandleCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToSnakeCase_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var input = "TestÄÖÜ";

        // Act
        var result = input.ToSnakeCase();

        // Assert
        // Unicode uppercase letters are treated as individual uppercase characters, so underscores are inserted
        result.Should().Be("test_ä_ö_ü");
    }

    [Fact]
    public void ToSnakeCase_WithLongString_ShouldPerformWell()
    {
        // Arrange
        var longInput = string.Concat(Enumerable.Repeat("VeryLongBusinessEntityName", 100));

        // Act
        var result = longInput.ToSnakeCase();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("very_long_business_entity_name");
        result.Should().EndWith("very_long_business_entity_name");
    }

    [Theory]
    [InlineData("TestCase", "test_case")]
    [InlineData("TestCASE", "test_c_a_s_e")]
    [InlineData("TESTCase", "t_e_s_t_case")]
    [InlineData("testCASE", "test_c_a_s_e")]
    public void ToSnakeCase_WithMixedCapitalization_ShouldHandleEdgeCases(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToSnakeCase_Performance_ShouldBeReasonable()
    {
        // Arrange
        var testStrings = new[]
        {
            "BusinessName",
            "InvoiceTransmissionQueue",
            "FIRSApiConfiguration",
            "UserRoleAssignment",
            "SystemConfiguration"
        };

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            foreach (var testString in testStrings)
            {
                _ = testString.ToSnakeCase();
            }
        }

        stopwatch.Stop();

        // Should complete in reasonable time (less than 1 second for 50,000 operations)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Theory]
    [InlineData(" ", " ")]
    [InlineData("  ", "  ")]
    [InlineData("\t", "\t")]
    [InlineData("\n", "\n")]
    [InlineData("\r", "\r")]
    public void ToSnakeCase_WithWhitespaceCharacters_ShouldPreserveWhitespace(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }
}