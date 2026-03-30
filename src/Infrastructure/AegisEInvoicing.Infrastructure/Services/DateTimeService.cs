using AegisEInvoicing.Application.Common.Interfaces;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Implementation of DateTime service
/// </summary>
public sealed class DateTimeService : IDateTime
{
    public DateTimeOffset Now => DateTimeOffset.Now;
    public DateTimeOffset UtcNow => DateTime.UtcNow;
    public DateTimeOffset Today => DateTime.Today;
    public DateOnly DateOnly => DateOnly.FromDateTime(DateTime.Now);
    public TimeOnly TimeOnly => TimeOnly.FromDateTime(DateTime.Now);
}