namespace AegisEInvoicing.Application.Features.BusinessManagement.DTOs;

/// <summary>
/// Represents an enum option with its value, name, and description
/// </summary>
public record FlowRuleEnumOptionDto
{
    /// <summary>
    /// The numeric value of the enum
    /// </summary>
    public int Value { get; init; }
    
    /// <summary>
    /// The name/label of the enum option
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// A description of what this enum option represents
    /// </summary>
    public string Description { get; init; } = string.Empty;
}
