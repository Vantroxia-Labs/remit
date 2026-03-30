namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// DateTimeOffset service interface for testability
/// </summary>
public interface IDateTime
{
    DateTimeOffset Now { get; }
    DateTimeOffset UtcNow { get; }
    DateTimeOffset Today { get; }
    DateOnly DateOnly { get; }
    TimeOnly TimeOnly { get; }
}