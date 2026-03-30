using AegisEInvoicing.Domain.Extensions;
using FluentAssertions;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Extensions;

/// <summary>
/// Comprehensive tests for DateOnlyJsonConverter targeting 100% code coverage
/// </summary>
public class DateOnlyJsonConverterTests
{
    private readonly JsonSerializerOptions _options;
    private readonly DateOnlyJsonConverter _converter;

    public DateOnlyJsonConverterTests()
    {
        _converter = new DateOnlyJsonConverter();
        _options = new JsonSerializerOptions();
        _options.Converters.Add(_converter);
    }

    #region Read Method Tests

    [Theory]
    [InlineData("\"2024-01-15\"", "2024-01-15")]
    [InlineData("\"2023-12-25\"", "2023-12-25")]
    [InlineData("\"2000-02-29\"", "2000-02-29")] // Leap year
    [InlineData("\"1999-01-01\"", "1999-01-01")]
    [InlineData("\"2024-12-31\"", "2024-12-31")]
    public void Read_WithValidDateFormat_ShouldReturnCorrectDate(string json, string expectedDateString)
    {
        // Act
        var result = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        var expectedDate = DateOnly.ParseExact(expectedDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        result.Should().Be(expectedDate);
    }

    [Fact]
    public void Read_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("\"\\t\"")]
    public void Read_WithEmptyOrWhitespaceString_ShouldReturnNull(string json)
    {
        // Act
        var result = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Read_WithNonStringToken_ShouldThrowJsonException(string json)
    {
        // Act & Assert
        var action = () => JsonSerializer.Deserialize<DateOnly?>(json, _options);
        action.Should().Throw<JsonException>()
            .WithMessage("Unexpected token parsing DateOnly. Expected String, got *");
    }

    [Theory]
    [InlineData("\"2024-1-15\"")]     // Single digit month
    [InlineData("\"2024-01-5\"")]     // Single digit day
    [InlineData("\"24-01-15\"")]      // Two digit year
    [InlineData("\"2024/01/15\"")]    // Wrong separator
    [InlineData("\"15-01-2024\"")]    // DD-MM-YYYY format
    [InlineData("\"01/15/2024\"")]    // MM/DD/YYYY format
    [InlineData("\"2024-13-01\"")]    // Invalid month
    [InlineData("\"2024-01-32\"")]    // Invalid day
    [InlineData("\"2024-02-30\"")]    // Invalid day for February
    [InlineData("\"2023-02-29\"")]    // Invalid leap year
    [InlineData("\"invalid-date\"")]  // Completely invalid
    [InlineData("\"2024-01-15T10:30:00\"")]  // DateTime format
    [InlineData("\"2024-01-15 10:30\"")]     // Date with time
    public void Read_WithInvalidDateFormat_ShouldThrowJsonException(string json)
    {
        // Act & Assert
        var action = () => JsonSerializer.Deserialize<DateOnly?>(json, _options);
        action.Should().Throw<JsonException>()
            .WithMessage("Invalid date format. Expected format is 'yyyy-MM-dd', but got *");
    }

    #endregion

    #region Write Method Tests

    [Theory]
    [InlineData("2024-01-15")]
    [InlineData("2023-12-25")]
    [InlineData("2000-02-29")]
    [InlineData("1999-01-01")]
    [InlineData("2024-12-31")]
    public void Write_WithValidDate_ShouldSerializeCorrectly(string dateString)
    {
        // Arrange
        var date = DateOnly.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Act
        var json = JsonSerializer.Serialize<DateOnly?>(date, _options);

        // Assert
        json.Should().Be($"\"{dateString}\"");
    }

    [Fact]
    public void Write_WithNullValue_ShouldSerializeAsNull()
    {
        // Arrange
        DateOnly? nullDate = null;

        // Act
        var json = JsonSerializer.Serialize(nullDate, _options);

        // Assert
        json.Should().Be("null");
    }

    [Fact]
    public void Write_WithMinDate_ShouldSerializeCorrectly()
    {
        // Arrange
        var minDate = DateOnly.MinValue;

        // Act
        var json = JsonSerializer.Serialize<DateOnly?>(minDate, _options);

        // Assert
        json.Should().Be("\"0001-01-01\"");
    }

    [Fact]
    public void Write_WithMaxDate_ShouldSerializeCorrectly()
    {
        // Arrange
        var maxDate = DateOnly.MaxValue;

        // Act
        var json = JsonSerializer.Serialize<DateOnly?>(maxDate, _options);

        // Assert
        json.Should().Be("\"9999-12-31\"");
    }

    #endregion

    #region Round-trip Tests

    [Theory]
    [InlineData("2024-01-15")]
    [InlineData("2023-12-25")]
    [InlineData("2000-02-29")]
    [InlineData("1999-01-01")]
    [InlineData("2024-12-31")]
    public void RoundTrip_WithValidDate_ShouldPreserveValue(string dateString)
    {
        // Arrange
        var originalDate = DateOnly.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Act
        var json = JsonSerializer.Serialize<DateOnly?>(originalDate, _options);
        var deserializedDate = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        deserializedDate.Should().Be(originalDate);
    }

    [Fact]
    public void RoundTrip_WithNullValue_ShouldPreserveNull()
    {
        // Arrange
        DateOnly? nullDate = null;

        // Act
        var json = JsonSerializer.Serialize(nullDate, _options);
        var deserializedDate = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        deserializedDate.Should().BeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_WithComplexObject_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var testObject = new TestObjectWithDate
        {
            Id = 1,
            Name = "Test",
            BirthDate = new DateOnly(1990, 5, 15),
            OptionalDate = new DateOnly(2024, 12, 25),
            NullDate = null
        };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);
        var deserializedObject = JsonSerializer.Deserialize<TestObjectWithDate>(json, _options);

        // Assert
        deserializedObject.Should().NotBeNull();
        deserializedObject!.Id.Should().Be(testObject.Id);
        deserializedObject.Name.Should().Be(testObject.Name);
        deserializedObject.BirthDate.Should().Be(testObject.BirthDate);
        deserializedObject.OptionalDate.Should().Be(testObject.OptionalDate);
        deserializedObject.NullDate.Should().BeNull();
    }

    [Fact]
    public void Integration_WithArrayOfDates_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var dates = new DateOnly?[]
        {
            new DateOnly(2024, 1, 1),
            null,
            new DateOnly(2024, 12, 31),
            new DateOnly(2000, 2, 29)
        };

        // Act
        var json = JsonSerializer.Serialize(dates, _options);
        var deserializedDates = JsonSerializer.Deserialize<DateOnly?[]>(json, _options);

        // Assert
        deserializedDates.Should().NotBeNull();
        deserializedDates.Should().HaveCount(4);
        deserializedDates![0].Should().Be(new DateOnly(2024, 1, 1));
        deserializedDates[1].Should().BeNull();
        deserializedDates[2].Should().Be(new DateOnly(2024, 12, 31));
        deserializedDates[3].Should().Be(new DateOnly(2000, 2, 29));
    }

    #endregion

    #region Converter Properties Tests

    [Fact]
    public void Converter_ShouldBeSealed()
    {
        // Act & Assert
        typeof(DateOnlyJsonConverter).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Converter_ShouldInheritFromJsonConverter()
    {
        // Act & Assert
        _converter.Should().BeAssignableTo<JsonConverter<DateOnly?>>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Read_WithVeryLongInvalidString_ShouldThrowJsonException()
    {
        // Arrange
        var longInvalidString = new string('x', 1000);
        var json = $"\"{longInvalidString}\"";

        // Act & Assert
        var action = () => JsonSerializer.Deserialize<DateOnly?>(json, _options);
        action.Should().Throw<JsonException>()
            .WithMessage($"Invalid date format. Expected format is 'yyyy-MM-dd', but got '{longInvalidString}'.");
    }

    [Theory]
    [InlineData("\"2024-01-15\"")]
    [InlineData("\"2000-02-29\"")]
    [InlineData("\"1900-01-01\"")]
    public void Read_WithDifferentValidYears_ShouldParseCorrectly(string json)
    {
        // Act
        var result = JsonSerializer.Deserialize<DateOnly?>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.Value.ToString("yyyy-MM-dd").Should().Be(json.Trim('"'));
    }

    [Fact]
    public void Write_ShouldUseInvariantCulture()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var date = new DateOnly(2024, 1, 15);

        try
        {
            // Set a culture that uses different date formats
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            // Act
            var json = JsonSerializer.Serialize<DateOnly?>(date, _options);

            // Assert
            json.Should().Be("\"2024-01-15\""); // Should still use ISO format, not German format
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region Test Helper Classes

    private class TestObjectWithDate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public DateOnly? OptionalDate { get; set; }
        public DateOnly? NullDate { get; set; }
    }

    #endregion
}