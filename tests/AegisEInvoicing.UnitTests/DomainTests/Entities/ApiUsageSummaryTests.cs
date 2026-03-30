using AegisEInvoicing.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class ApiUsageSummaryTests
{
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly DateTimeOffset _periodStart = DateTimeOffset.UtcNow.Date;
    private readonly DateTimeOffset _periodEnd;

    public ApiUsageSummaryTests()
    {
        _periodEnd = _periodStart.AddDays(30);
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateApiUsageSummary()
    {
        // Act
        var summary = ApiUsageSummary.Create(_businessId, _periodStart, _periodEnd);

        // Assert
        summary.Should().NotBeNull();
        summary.Id.Should().NotBeEmpty();
        summary.BusinessId.Should().Be(_businessId);
        summary.PeriodStart.Should().Be(_periodStart);
        summary.PeriodEnd.Should().Be(_periodEnd);
        summary.TotalRequests.Should().Be(0);
        summary.SuccessfulRequests.Should().Be(0);
        summary.FailedRequests.Should().Be(0);
        summary.TotalDataTransferredBytes.Should().Be(0);
        summary.TotalCost.Should().Be(0);
        summary.EndpointUsage.Should().BeEmpty();
        summary.EndpointCosts.Should().BeEmpty();
        summary.FIRSOperationsCount.Should().Be(0);
        summary.FIRSOperationsCost.Should().Be(0);
        summary.IsFinalized.Should().BeFalse();
        summary.FinalizedAt.Should().BeNull();
        summary.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateFromUsageRecords_WithEmptyCollection_ShouldSetZeroValues()
    {
        // Arrange
        var summary = CreateTestApiUsageSummary();
        var usageRecords = new List<ApiUsageTracking>();

        // Act
        summary.UpdateFromUsageRecords(usageRecords);

        // Assert
        summary.TotalRequests.Should().Be(0);
        summary.SuccessfulRequests.Should().Be(0);
        summary.FailedRequests.Should().Be(0);
        summary.TotalDataTransferredBytes.Should().Be(0);
        summary.TotalCost.Should().Be(0);
        summary.EndpointUsage.Should().BeEmpty();
        summary.EndpointCosts.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromUsageRecords_WithUsageRecords_ShouldCalculateCorrectTotals()
    {
        // Arrange
        var summary = CreateTestApiUsageSummary();
        var usageRecords = new List<ApiUsageTracking>
        {
            CreateMockApiUsageTracking("/api/invoices", 200, 1000, 2000, 5.0m, true),
            CreateMockApiUsageTracking("/api/invoices", 201, 1500, 3000, 7.5m, true),
            CreateMockApiUsageTracking("/api/users", 400, 500, 1000, 0m, false),
            CreateMockApiUsageTracking("/api/users", 500, 800, 1500, 0m, false)
        };

        // Act
        summary.UpdateFromUsageRecords(usageRecords);

        // Assert
        summary.TotalRequests.Should().Be(4);
        summary.SuccessfulRequests.Should().Be(2); // 200, 201
        summary.FailedRequests.Should().Be(2); // 400, 500
        // Total: (1000+2000) + (1500+3000) + (500+1000) + (800+1500) = 11300
        summary.TotalDataTransferredBytes.Should().Be(11300); // Sum of request + response sizes
        summary.TotalCost.Should().Be(12.5m); // Only billable records
    }

    [Fact]
    public void UpdateFromUsageRecords_ShouldGroupEndpointUsageCorrectly()
    {
        // Arrange
        var summary = CreateTestApiUsageSummary();
        var usageRecords = new List<ApiUsageTracking>
        {
            CreateMockApiUsageTracking("/api/invoices", 200, 1000, 2000, 5.0m, true),
            CreateMockApiUsageTracking("/api/invoices", 201, 1500, 3000, 7.5m, true),
            CreateMockApiUsageTracking("/api/users", 200, 500, 1000, 2.0m, true),
        };

        // Act
        summary.UpdateFromUsageRecords(usageRecords);

        // Assert
        summary.EndpointUsage.Should().HaveCount(2);
        summary.EndpointUsage["/api/invoices"].Should().Be(2);
        summary.EndpointUsage["/api/users"].Should().Be(1);

        summary.EndpointCosts.Should().HaveCount(2);
        summary.EndpointCosts["/api/invoices"].Should().Be(12.5m);
        summary.EndpointCosts["/api/users"].Should().Be(2.0m);
    }

    [Fact]
    public void UpdateFromUsageRecords_ShouldIgnoreNonBillableRecordsInCostCalculation()
    {
        // Arrange
        var summary = CreateTestApiUsageSummary();
        var usageRecords = new List<ApiUsageTracking>
        {
            CreateMockApiUsageTracking("/api/invoices", 200, 1000, 2000, 5.0m, true),
            CreateMockApiUsageTracking("/api/invoices", 200, 1000, 2000, 10.0m, false), // Non-billable
        };

        // Act
        summary.UpdateFromUsageRecords(usageRecords);

        // Assert
        summary.TotalCost.Should().Be(5.0m);
        summary.EndpointCosts["/api/invoices"].Should().Be(5.0m);
    }

    [Fact]
    public void FinalizeSummary_WhenNotFinalized_ShouldFinalizeSummary()
    {
        // Arrange
        var summary = CreateTestApiUsageSummary();

        // Act
        summary.FinalizeSummary();

        // Assert
        summary.IsFinalized.Should().BeTrue();
        summary.FinalizedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FinalizeSummary_WhenAlreadyFinalized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var summary = CreateTestApiUsageSummary();
        summary.FinalizeSummary();

        // Act & Assert
        var action = () => summary.FinalizeSummary();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Summary is already finalized");
    }

    [Fact]
    public void UpdateFromUsageRecords_WhenFinalized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var summary = CreateTestApiUsageSummary();
        summary.FinalizeSummary();
        var usageRecords = new List<ApiUsageTracking>();

        // Act & Assert
        var action = () => summary.UpdateFromUsageRecords(usageRecords);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update finalized summary");
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, true)]
    [InlineData(299, true)]
    [InlineData(300, false)]
    [InlineData(400, false)]
    [InlineData(500, false)]
    public void UpdateFromUsageRecords_ShouldCountSuccessfulRequestsCorrectly(int statusCode, bool shouldBeSuccessful)
    {
        // Arrange
        var summary = CreateTestApiUsageSummary();
        var usageRecords = new List<ApiUsageTracking>
        {
            CreateMockApiUsageTracking("/api/test", statusCode, 1000, 2000, 1.0m, true)
        };

        // Act
        summary.UpdateFromUsageRecords(usageRecords);

        // Assert
        summary.TotalRequests.Should().Be(1);
        if (shouldBeSuccessful)
        {
            summary.SuccessfulRequests.Should().Be(1);
            summary.FailedRequests.Should().Be(0);
        }
        else
        {
            summary.SuccessfulRequests.Should().Be(0);
            summary.FailedRequests.Should().Be(1);
        }
    }

    private ApiUsageSummary CreateTestApiUsageSummary()
    {
        return ApiUsageSummary.Create(_businessId, _periodStart, _periodEnd);
    }

    private ApiUsageTracking CreateMockApiUsageTracking(
        string endpoint,
        int responseStatusCode,
        long requestSize,
        long responseSize,
        decimal cost,
        bool isBillable)
    {
        var apiUsage = ApiUsageTracking.Create(
            _businessId,
            endpoint,
            "GET",
            DateTimeOffset.UtcNow);

        // Use RecordResponse to set response properties
        apiUsage.RecordResponse(responseStatusCode, 100, requestSize, responseSize);

        // Use RecordFIRSOperation to set cost (cost is calculated but we override via reflection if needed)
        apiUsage.RecordFIRSOperation(null, false);

        // Set Cost and IsBillable via reflection (these are private setters)
        typeof(ApiUsageTracking).GetProperty("Cost")!.SetValue(apiUsage, cost);
        typeof(ApiUsageTracking).GetProperty("IsBillable")!.SetValue(apiUsage, isBillable);

        return apiUsage;
    }
}