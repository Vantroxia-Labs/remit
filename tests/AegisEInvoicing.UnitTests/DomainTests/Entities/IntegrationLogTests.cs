using AegisEInvoicing.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class IntegrationLogTests
{
    [Fact]
    public void Create_WithMinimalParameters_ShouldCreateIntegrationLog()
    {
        // Arrange
        var operation = "CreateInvoice";
        var externalSystem = "FIRS";
        var requestData = "{ \"invoice\": { \"id\": \"123\" } }";

        // Act
        var integrationLog = IntegrationLog.Create(operation, externalSystem, requestData);

        // Assert
        integrationLog.Should().NotBeNull();
        integrationLog.Id.Should().NotBeEmpty();
        integrationLog.Operation.Should().Be(operation);
        integrationLog.ExternalSystem.Should().Be(externalSystem);
        integrationLog.RequestData.Should().Be(requestData);
        integrationLog.CorrelationId.Should().BeNull();
        integrationLog.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        integrationLog.IsSuccess.Should().BeFalse();
        integrationLog.CompletedAt.Should().BeNull();
        integrationLog.DurationMs.Should().BeNull();
        integrationLog.ResponseData.Should().BeNull();
        integrationLog.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var operation = "CreateInvoice";
        var externalSystem = "FIRS";
        var requestData = "{ \"invoice\": { \"id\": \"123\" } }";
        var correlationId = "correlation-123";

        // Act
        var integrationLog = IntegrationLog.Create(operation, externalSystem, requestData, correlationId);

        // Assert
        integrationLog.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void MarkAsCompleted_WithSuccessfulResponse_ShouldUpdateLog()
    {
        // Arrange
        var integrationLog = CreateTestIntegrationLog();
        var responseData = "{ \"success\": true, \"invoiceNumber\": \"INV-001\" }";

        // Add a small delay to ensure duration is calculated
        System.Threading.Thread.Sleep(10);

        // Act
        integrationLog.MarkAsCompleted(responseData, true);

        // Assert
        integrationLog.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        integrationLog.DurationMs.Should().BeGreaterThan(0);
        integrationLog.ResponseData.Should().Be(responseData);
        integrationLog.IsSuccess.Should().BeTrue();
        integrationLog.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsCompleted_WithFailedResponse_ShouldUpdateLogWithError()
    {
        // Arrange
        var integrationLog = CreateTestIntegrationLog();
        var responseData = "{ \"error\": \"Invalid request\" }";
        var errorMessage = "Request validation failed";

        // Add a small delay to ensure duration is calculated
        System.Threading.Thread.Sleep(10);

        // Act
        integrationLog.MarkAsCompleted(responseData, false, errorMessage);

        // Assert
        integrationLog.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        integrationLog.DurationMs.Should().BeGreaterThan(0);
        integrationLog.ResponseData.Should().Be(responseData);
        integrationLog.IsSuccess.Should().BeFalse();
        integrationLog.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void MarkAsCompleted_WithMinimalParameters_ShouldUpdateLog()
    {
        // Arrange
        var integrationLog = CreateTestIntegrationLog();

        // Add a small delay to ensure duration is calculated
        System.Threading.Thread.Sleep(10);

        // Act
        integrationLog.MarkAsCompleted();

        // Assert
        integrationLog.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        integrationLog.DurationMs.Should().BeGreaterThan(0);
        integrationLog.ResponseData.Should().BeNull();
        integrationLog.IsSuccess.Should().BeTrue();
        integrationLog.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsCompleted_ShouldCalculateDurationCorrectly()
    {
        // Arrange
        var integrationLog = CreateTestIntegrationLog();
        var startTime = integrationLog.StartedAt;

        // Add a measurable delay
        System.Threading.Thread.Sleep(50);

        // Act
        integrationLog.MarkAsCompleted();

        // Assert
        integrationLog.DurationMs.Should().BeGreaterThan(30); // Allow for some variance
        integrationLog.DurationMs.Should().BeLessThan(200); // But not too much

        var expectedDuration = (long)(integrationLog.CompletedAt!.Value - startTime).TotalMilliseconds;
        integrationLog.DurationMs.Should().Be(expectedDuration);
    }

    [Theory]
    [InlineData("CreateInvoice", "FIRS")]
    [InlineData("UpdateBusiness", "KYC")]
    [InlineData("SendNotification", "EmailService")]
    public void Create_WithDifferentOperationsAndSystems_ShouldWork(string operation, string externalSystem)
    {
        // Arrange
        var requestData = "{ \"test\": \"data\" }";

        // Act
        var integrationLog = IntegrationLog.Create(operation, externalSystem, requestData);

        // Assert
        integrationLog.Operation.Should().Be(operation);
        integrationLog.ExternalSystem.Should().Be(externalSystem);
    }

    [Fact]
    public void Constructor_ShouldInitializeDefaultValues()
    {
        // Act
        var integrationLog = new IntegrationLog();

        // Assert
        integrationLog.Operation.Should().Be(string.Empty);
        integrationLog.ExternalSystem.Should().Be(string.Empty);
        integrationLog.RequestData.Should().Be(string.Empty);
        integrationLog.IsSuccess.Should().BeFalse();
    }

    private IntegrationLog CreateTestIntegrationLog()
    {
        return IntegrationLog.Create(
            "TestOperation",
            "TestSystem",
            "{ \"test\": \"data\" }",
            "test-correlation-id");
    }
}