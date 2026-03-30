using AegisEInvoicing.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.InfrastructureTests.Services;

public class DateTimeServiceTests
{
    private readonly DateTimeService _dateTimeService;

    public DateTimeServiceTests()
    {
        _dateTimeService = new DateTimeService();
    }

    [Fact]
    public void Now_ShouldReturnCurrentDateTimeOffset()
    {
        // Act
        var result = _dateTimeService.Now;

        // Assert
        result.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcDateTimeOffset()
    {
        // Act
        var result = _dateTimeService.UtcNow;

        // Assert
        result.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Today_ShouldReturnTodaysDate()
    {
        // Act
        var result = _dateTimeService.Today;

        // Assert
        result.Date.Should().Be(DateTime.Today);
    }

    [Fact]
    public void DateOnly_ShouldReturnCurrentDateOnly()
    {
        // Act
        var result = _dateTimeService.DateOnly;

        // Assert
        result.Should().Be(DateOnly.FromDateTime(DateTime.Now));
    }

    [Fact]
    public void TimeOnly_ShouldReturnCurrentTimeOnly()
    {
        // Act
        var result = _dateTimeService.TimeOnly;

        // Assert
        result.Should().BeCloseTo(TimeOnly.FromDateTime(DateTime.Now), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Properties_ShouldBeConsistent()
    {
        // Arrange
        var startTime = DateTime.Now;

        // Act
        var now = _dateTimeService.Now;
        var utcNow = _dateTimeService.UtcNow;
        var today = _dateTimeService.Today;
        var dateOnly = _dateTimeService.DateOnly;
        var timeOnly = _dateTimeService.TimeOnly;

        var endTime = DateTime.Now;

        // Assert
        now.Should().BeAfter(startTime.AddMilliseconds(-1));
        now.Should().BeBefore(endTime.AddMilliseconds(1));

        utcNow.Should().BeAfter(startTime.ToUniversalTime().AddMilliseconds(-1));
        utcNow.Should().BeBefore(endTime.ToUniversalTime().AddMilliseconds(1));

        today.Date.Should().Be(DateTime.Today);
        dateOnly.Should().Be(DateOnly.FromDateTime(startTime));
        timeOnly.Should().BeCloseTo(TimeOnly.FromDateTime(startTime), TimeSpan.FromSeconds(1));
    }
}