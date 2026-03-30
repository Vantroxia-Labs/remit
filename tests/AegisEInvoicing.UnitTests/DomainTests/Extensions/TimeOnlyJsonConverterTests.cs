using AegisEInvoicing.Domain.Extensions;
using FluentAssertions;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Extensions;

/// <summary>
/// Comprehensive tests for TimeOnlyJsonConverter targeting 100% code coverage
/// </summary>
public class TimeOnlyJsonConverterTests
{
    private readonly JsonSerializerOptions _options;
    private readonly TimeOnlyJsonConverter _converter;

    public TimeOnlyJsonConverterTests()
    {
        _converter = new TimeOnlyJsonConverter();
        _options = new JsonSerializerOptions();
        _options.Converters.Add(_converter);
    }

    #region Read Method Tests

    [Theory]
    [InlineData("\"00:00:00\"", 0, 0, 0)]
    [InlineData("\"12:30:45\"", 12, 30, 45)]
    [InlineData("\"23:59:59\"", 23, 59, 59)]
    [InlineData("\"08:15:30\"", 8, 15, 30)]
    [InlineData("\"17:45:00\"", 17, 45, 0)]
    [InlineData("\"01:05:09\"", 1, 5, 9)]
    public void Read_WithValidTimeFormat_ShouldReturnCorrectTime(string json, int hour, int minute, int second)
    {
        // Act
        var result = JsonSerializer.Deserialize<TimeOnly?>(json, _options);

        // Assert
        var expectedTime = new TimeOnly(hour, minute, second);
        result.Should().Be(expectedTime);
    }

    [Fact]
    public void Read_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<TimeOnly?>(json, _options);

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
        var result = JsonSerializer.Deserialize<TimeOnly?>(json, _options);

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
        var action = () => JsonSerializer.Deserialize<TimeOnly?>(json, _options);
        action.Should().Throw<JsonException>()
            .WithMessage("Unexpected token parsing TimeOnly. Expected String, got *");
    }

    [Theory]
    [InlineData("\"12:30\"")]           // Missing seconds
    [InlineData("\"12:30:45:123\"")]   // With milliseconds
    [InlineData("\"12:30:45.123\"")]   // With fractional seconds
    [InlineData("\"12:30 PM\"")]       // 12-hour format
    [InlineData("\"12:30:45 AM\"")]    // 12-hour format with seconds
    [InlineData("\"24:00:00\"")]       // Invalid hour
    [InlineData("\"12:60:00\"")]       // Invalid minute
    [InlineData("\"12:30:60\"")]       // Invalid second
    [InlineData("\"25:30:45\"")]       // Hour > 24
    [InlineData("\"12:65:45\"")]       // Minute > 59
    [InlineData("\"12:30:75\"")]       // Second > 59
    [InlineData("\"invalid-time\"")]   // Completely invalid
    [InlineData("\"12-30-45\"")]       // Wrong separator
    [InlineData("\"12.30.45\"")]       // Wrong separator
    [InlineData("\"12/30/45\"")]       // Wrong separator
    [InlineData("\"1:30:45\"")]        // Single digit hour
    [InlineData("\"12:3:45\"")]        // Single digit minute
    [InlineData("\"12:30:4\"")]        // Single digit second
    public void Read_WithInvalidTimeFormat_ShouldThrowJsonException(string json)
    {
        // Act & Assert
        var action = () => JsonSerializer.Deserialize<TimeOnly?>(json, _options);
        action.Should().Throw<JsonException>()
            .WithMessage("Invalid time format. Expected format is 'HH:mm:ss', but got *");
    }

    #endregion

    #region Write Method Tests

    [Theory]
    [InlineData(0, 0, 0, "00:00:00")]
    [InlineData(12, 30, 45, "12:30:45")]
    [InlineData(23, 59, 59, "23:59:59")]
    [InlineData(8, 15, 30, "08:15:30")]
    [InlineData(17, 45, 0, "17:45:00")]
    [InlineData(1, 5, 9, "01:05:09")]
    public void Write_WithValidTime_ShouldSerializeCorrectly(int hour, int minute, int second, string expectedTime)
    {
        // Arrange
        var time = new TimeOnly(hour, minute, second);

        // Act
        var json = JsonSerializer.Serialize<TimeOnly?>(time, _options);

        // Assert
        json.Should().Be($"\"{expectedTime}\"");
    }

    [Fact]
    public void Write_WithNullValue_ShouldSerializeAsNull()
    {
        // Arrange
        TimeOnly? nullTime = null;

        // Act
        var json = JsonSerializer.Serialize(nullTime, _options);

        // Assert
        json.Should().Be("null");
    }

    [Fact]
    public void Write_WithMinTime_ShouldSerializeCorrectly()
    {
        // Arrange
        var minTime = TimeOnly.MinValue;

        // Act
        var json = JsonSerializer.Serialize<TimeOnly?>(minTime, _options);

        // Assert
        json.Should().Be("\"00:00:00\"");
    }

    [Fact]
    public void Write_WithMaxTime_ShouldSerializeCorrectly()
    {
        // Arrange
        var maxTime = TimeOnly.MaxValue;

        // Act
        var json = JsonSerializer.Serialize<TimeOnly?>(maxTime, _options);

        // Assert
        json.Should().Be("\"23:59:59\"");
    }

    [Fact]
    public void Write_WithMidnight_ShouldSerializeCorrectly()
    {
        // Arrange
        var midnight = new TimeOnly(0, 0, 0);

        // Act
        var json = JsonSerializer.Serialize<TimeOnly?>(midnight, _options);

        // Assert
        json.Should().Be("\"00:00:00\"");
    }

    [Fact]
    public void Write_WithNoon_ShouldSerializeCorrectly()
    {
        // Arrange
        var noon = new TimeOnly(12, 0, 0);

        // Act
        var json = JsonSerializer.Serialize<TimeOnly?>(noon, _options);

        // Assert
        json.Should().Be("\"12:00:00\"");
    }

    #endregion

    #region Round-trip Tests

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(12, 30, 45)]
    [InlineData(23, 59, 59)]
    [InlineData(8, 15, 30)]
    [InlineData(17, 45, 0)]
    [InlineData(1, 5, 9)]
    public void RoundTrip_WithValidTime_ShouldPreserveValue(int hour, int minute, int second)
    {
        // Arrange
        var originalTime = new TimeOnly(hour, minute, second);

        // Act
        var json = JsonSerializer.Serialize<TimeOnly?>(originalTime, _options);
        var deserializedTime = JsonSerializer.Deserialize<TimeOnly?>(json, _options);

        // Assert
        deserializedTime.Should().Be(originalTime);
    }

    [Fact]
    public void RoundTrip_WithNullValue_ShouldPreserveNull()
    {
        // Arrange
        TimeOnly? nullTime = null;

        // Act
        var json = JsonSerializer.Serialize(nullTime, _options);
        var deserializedTime = JsonSerializer.Deserialize<TimeOnly?>(json, _options);

        // Assert
        deserializedTime.Should().BeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_WithComplexObject_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var testObject = new TestObjectWithTime
        {
            Id = 1,
            Name = "Meeting",
            StartTime = new TimeOnly(9, 30, 0),
            EndTime = new TimeOnly(17, 30, 0),
            OptionalTime = new TimeOnly(12, 0, 0),
            NullTime = null
        };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);
        var deserializedObject = JsonSerializer.Deserialize<TestObjectWithTime>(json, _options);

        // Assert
        deserializedObject.Should().NotBeNull();
        deserializedObject!.Id.Should().Be(testObject.Id);
        deserializedObject.Name.Should().Be(testObject.Name);
        deserializedObject.StartTime.Should().Be(testObject.StartTime);
        deserializedObject.EndTime.Should().Be(testObject.EndTime);
        deserializedObject.OptionalTime.Should().Be(testObject.OptionalTime);
        deserializedObject.NullTime.Should().BeNull();
    }

    [Fact]
    public void Integration_WithArrayOfTimes_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var times = new TimeOnly?[]
        {
            new TimeOnly(8, 0, 0),
            null,
            new TimeOnly(12, 30, 15),
            new TimeOnly(18, 45, 30)
        };

        // Act
        var json = JsonSerializer.Serialize(times, _options);
        var deserializedTimes = JsonSerializer.Deserialize<TimeOnly?[]>(json, _options);

        // Assert
        deserializedTimes.Should().NotBeNull();
        deserializedTimes.Should().HaveCount(4);
        deserializedTimes![0].Should().Be(new TimeOnly(8, 0, 0));
        deserializedTimes[1].Should().BeNull();
        deserializedTimes[2].Should().Be(new TimeOnly(12, 30, 15));
        deserializedTimes[3].Should().Be(new TimeOnly(18, 45, 30));
    }

    #endregion

    #region Converter Properties Tests

    [Fact]
    public void Converter_ShouldBeSealed()
    {
        // Act & Assert
        typeof(TimeOnlyJsonConverter).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Converter_ShouldInheritFromJsonConverter()
    {
        // Act & Assert
        _converter.Should().BeAssignableTo<JsonConverter<TimeOnly?>>();
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
        var action = () => JsonSerializer.Deserialize<TimeOnly?>(json, _options);
        action.Should().Throw<JsonException>()
            .WithMessage($"Invalid time format. Expected format is 'HH:mm:ss', but got '{longInvalidString}'.");
    }

    [Theory]
    [InlineData("\"00:00:00\"")]
    [InlineData("\"12:00:00\"")]
    [InlineData("\"23:59:59\"")]
    public void Read_WithDifferentValidTimes_ShouldParseCorrectly(string json)
    {
        // Act
        var result = JsonSerializer.Deserialize<TimeOnly?>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.Value.ToString("HH:mm:ss").Should().Be(json.Trim('"'));
    }

    [Fact]
    public void Write_ShouldUseInvariantCulture()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var time = new TimeOnly(14, 30, 45);

        try
        {
            // Set a culture that uses different time formats
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            // Act
            var json = JsonSerializer.Serialize<TimeOnly?>(time, _options);

            // Assert
            json.Should().Be("\"14:30:45\""); // Should use 24-hour format regardless of culture
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData(0, 0, 0)]     // Start of day
    [InlineData(11, 59, 59)]  // End of morning
    [InlineData(12, 0, 0)]    // Noon
    [InlineData(12, 0, 1)]    // Start of afternoon
    [InlineData(23, 59, 58)]  // Almost end of day
    [InlineData(23, 59, 59)]  // End of day
    public void RoundTrip_WithBoundaryTimes_ShouldPreserveValue(int hour, int minute, int second)
    {
        // Arrange
        var time = new TimeOnly(hour, minute, second);

        // Act
        var json = JsonSerializer.Serialize<TimeOnly?>(time, _options);
        var deserializedTime = JsonSerializer.Deserialize<TimeOnly?>(json, _options);

        // Assert
        deserializedTime.Should().Be(time);
    }

    #endregion

    #region Test Helper Classes

    private class TestObjectWithTime
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public TimeOnly? OptionalTime { get; set; }
        public TimeOnly? NullTime { get; set; }
    }

    #endregion
}