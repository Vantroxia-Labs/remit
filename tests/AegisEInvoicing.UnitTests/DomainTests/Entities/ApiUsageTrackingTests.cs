using AegisEInvoicing.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class ApiUsageTrackingTests
{
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _requestTimestamp = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithMinimalParameters_ShouldCreateApiUsageTracking()
    {
        // Arrange
        var endpoint = "/api/invoices";
        var httpMethod = "POST";

        // Act
        var apiUsage = ApiUsageTracking.Create(
            _businessId,
            endpoint,
            httpMethod,
            _requestTimestamp);

        // Assert
        apiUsage.Should().NotBeNull();
        apiUsage.Id.Should().NotBeEmpty();
        apiUsage.BusinessId.Should().Be(_businessId);
        apiUsage.Endpoint.Should().Be(endpoint);
        apiUsage.HttpMethod.Should().Be(httpMethod);
        apiUsage.RequestTimestamp.Should().Be(_requestTimestamp);
        apiUsage.UserId.Should().BeNull();
        apiUsage.IpAddress.Should().BeNull();
        apiUsage.UserAgent.Should().BeNull();
        apiUsage.ApiKeyUsed.Should().BeNull();
        apiUsage.IsBillable.Should().BeTrue();
        apiUsage.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateApiUsageTracking()
    {
        // Arrange
        var endpoint = "/api/invoices";
        var httpMethod = "POST";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var apiKeyUsed = "api-key-123";

        // Act
        var apiUsage = ApiUsageTracking.Create(
            _businessId,
            endpoint,
            httpMethod,
            _requestTimestamp,
            _userId,
            ipAddress,
            userAgent,
            apiKeyUsed);

        // Assert
        apiUsage.UserId.Should().Be(_userId);
        apiUsage.IpAddress.Should().Be(ipAddress);
        apiUsage.UserAgent.Should().Be(userAgent);
        apiUsage.ApiKeyUsed.Should().Be(apiKeyUsed);
    }

    [Fact]
    public void RecordResponse_WithValidParameters_ShouldUpdateResponseData()
    {
        // Arrange
        var apiUsage = CreateTestApiUsageTracking();
        var statusCode = 200;
        var responseTimeMs = 150L;
        var requestSizeBytes = 1024L;
        var responseSizeBytes = 2048L;

        // Act
        apiUsage.RecordResponse(statusCode, responseTimeMs, requestSizeBytes, responseSizeBytes);

        // Assert
        apiUsage.ResponseStatusCode.Should().Be(statusCode);
        apiUsage.ResponseTimeMs.Should().Be(responseTimeMs);
        apiUsage.RequestSizeBytes.Should().Be(requestSizeBytes);
        apiUsage.ResponseSizeBytes.Should().Be(responseSizeBytes);
    }

    [Fact]
    public void RecordFIRSOperation_WithInvoiceId_ShouldUpdateFIRSData()
    {
        // Arrange
        var apiUsage = CreateTestApiUsageTracking();
        var invoiceId = "INV-12345";
        var usedAegisCredentials = true;

        // Act
        apiUsage.RecordFIRSOperation(invoiceId, usedAegisCredentials);

        // Assert
        apiUsage.FIRSInvoiceId.Should().Be(invoiceId);
        apiUsage.UsedAegisCredentials.Should().Be(usedAegisCredentials);
        apiUsage.Cost.Should().Be(0); // Cost calculation logic
    }

    [Fact]
    public void RecordFIRSOperation_WithoutInvoiceId_ShouldSetNullInvoiceId()
    {
        // Arrange
        var apiUsage = CreateTestApiUsageTracking();
        var usedAegisCredentials = false;

        // Act
        apiUsage.RecordFIRSOperation(null, usedAegisCredentials);

        // Assert
        apiUsage.FIRSInvoiceId.Should().BeNull();
        apiUsage.UsedAegisCredentials.Should().BeFalse();
        apiUsage.Cost.Should().Be(0);
    }

    [Fact]
    public void MarkAsNonBillable_ShouldSetBillableToFalseAndZeroCost()
    {
        // Arrange
        var apiUsage = CreateTestApiUsageTracking();

        // Act
        apiUsage.MarkAsNonBillable("Test operation");

        // Assert
        apiUsage.IsBillable.Should().BeFalse();
        apiUsage.Cost.Should().Be(0);
    }

    [Fact]
    public void MarkAsNonBillable_WithoutReason_ShouldStillWork()
    {
        // Arrange
        var apiUsage = CreateTestApiUsageTracking();

        // Act
        apiUsage.MarkAsNonBillable();

        // Assert
        apiUsage.IsBillable.Should().BeFalse();
        apiUsage.Cost.Should().Be(0);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public void Create_WithDifferentHttpMethods_ShouldWork(string httpMethod)
    {
        // Act
        var apiUsage = ApiUsageTracking.Create(
            _businessId,
            "/api/test",
            httpMethod,
            _requestTimestamp);

        // Assert
        apiUsage.HttpMethod.Should().Be(httpMethod);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(500)]
    public void RecordResponse_WithDifferentStatusCodes_ShouldWork(int statusCode)
    {
        // Arrange
        var apiUsage = CreateTestApiUsageTracking();

        // Act
        apiUsage.RecordResponse(statusCode, 100, 500, 1000);

        // Assert
        apiUsage.ResponseStatusCode.Should().Be(statusCode);
    }

    private ApiUsageTracking CreateTestApiUsageTracking()
    {
        return ApiUsageTracking.Create(
            _businessId,
            "/api/test",
            "GET",
            _requestTimestamp);
    }
}