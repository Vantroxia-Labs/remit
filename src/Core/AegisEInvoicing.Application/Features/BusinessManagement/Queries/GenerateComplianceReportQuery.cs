using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries;

public record GenerateComplianceReportQuery : IRequest<BusinessComplianceReportDto>
{
    public Guid BusinessId { get; init; }
}

public record BusinessComplianceReportDto
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = default!;
    public DateTimeOffset ReportGeneratedAt { get; init; }
    public int ComplianceScore { get; init; }
    public bool HasValidFIRSConnection { get; init; }
    public bool HasActiveSubscription { get; init; }
    public DateTimeOffset? LastAuditDate { get; init; }
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}