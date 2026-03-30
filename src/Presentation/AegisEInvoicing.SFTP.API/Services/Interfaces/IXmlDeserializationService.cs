using AegisEInvoicing.ERP.API.Models;

namespace AegisEInvoicing.SFTP.API.Services.Interfaces;

/// <summary>
/// Interface for XML deserialization operations
/// </summary>
public interface IXmlDeserializationService
{
    /// <summary>
    /// Deserializes XML content into a CreateInvoiceRequest object
    /// </summary>
    /// <param name="xmlContent">XML content to deserialize</param>
    /// <param name="fileName">Source file name for error reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized CreateInvoiceRequest or null if deserialization fails</returns>
    Task<CreateInvoiceRequest?> DeserializeInvoiceRequestAsync(
        string xmlContent, 
        string fileName, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the XML content structure before deserialization
    /// </summary>
    /// <param name="xmlContent">XML content to validate</param>
    /// <param name="fileName">Source file name for error reporting</param>
    /// <returns>Validation result with errors if any</returns>
    Task<XmlValidationResult> ValidateXmlStructureAsync(
        string xmlContent, 
        string fileName);
    
    /// <summary>
    /// Validates a deserialized CreateInvoiceRequest for business rule compliance
    /// </summary>
    /// <param name="invoiceRequest">The invoice request to validate</param>
    /// <param name="fileName">Source file name for error reporting</param>
    /// <returns>Validation result with errors if any</returns>
    Task<ValidationResult> ValidateInvoiceRequestAsync(
        CreateInvoiceRequest invoiceRequest, 
        string fileName);
    
    /// <summary>
    /// Extracts business ID from XML content if available
    /// </summary>
    /// <param name="xmlContent">XML content to parse</param>
    /// <returns>Business ID if found, null otherwise</returns>
    Guid? ExtractBusinessIdFromXml(string xmlContent);
    
    /// <summary>
    /// Gets supported XML schema versions
    /// </summary>
    /// <returns>List of supported XML schema versions</returns>
    List<string> GetSupportedSchemaVersions();
}

/// <summary>
/// Represents the result of XML validation
/// </summary>
public class XmlValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? SchemaVersion { get; set; }
    public string? DocumentType { get; set; }
    
    public static XmlValidationResult Success(string? schemaVersion = null, string? documentType = null)
    {
        return new XmlValidationResult 
        { 
            IsValid = true, 
            SchemaVersion = schemaVersion,
            DocumentType = documentType
        };
    }
    
    public static XmlValidationResult Failure(List<string> errors)
    {
        return new XmlValidationResult 
        { 
            IsValid = false, 
            Errors = errors 
        };
    }
    
    public static XmlValidationResult Failure(string error)
    {
        return new XmlValidationResult 
        { 
            IsValid = false, 
            Errors = new List<string> { error } 
        };
    }
}

/// <summary>
/// Represents the result of business rule validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }
    
    public static ValidationResult Failure(List<ValidationError> errors)
    {
        return new ValidationResult 
        { 
            IsValid = false, 
            Errors = errors 
        };
    }
    
    public static ValidationResult Failure(ValidationError error)
    {
        return new ValidationResult 
        { 
            IsValid = false, 
            Errors = new List<ValidationError> { error } 
        };
    }
}

/// <summary>
/// Represents a validation error
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public object? InvalidValue { get; set; }
    
    public ValidationError(string field, string message, string errorCode = "", object? invalidValue = null)
    {
        Field = field;
        Message = message;
        ErrorCode = errorCode;
        InvalidValue = invalidValue;
    }
}

/// <summary>
/// Represents a validation warning
/// </summary>
public class ValidationWarning
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string WarningCode { get; set; } = string.Empty;
    
    public ValidationWarning(string field, string message, string warningCode = "")
    {
        Field = field;
        Message = message;
        WarningCode = warningCode;
    }
}