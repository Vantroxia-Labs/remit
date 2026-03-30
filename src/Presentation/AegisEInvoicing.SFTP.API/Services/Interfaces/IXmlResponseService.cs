using AegisEInvoicing.SFTP.API.Models;

namespace AegisEInvoicing.SFTP.API.Services.Interfaces;

/// <summary>
/// Interface for generating XML response files (ACK/NACK)
/// </summary>
public interface IXmlResponseService
{
    /// <summary>
    /// Generates an ACK (Acknowledgment) XML file for successful invoice processing
    /// </summary>
    /// <param name="invoiceDetails">Invoice processing details</param>
    /// <param name="originalFileName">Original XML file name</param>
    /// <param name="connectionId">SFTP connection identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated XML response</returns>
    Task<XmlResponse> GenerateAckResponseAsync(
        InvoiceDetails invoiceDetails, 
        string originalFileName, 
        string connectionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a NACK (Negative Acknowledgment) XML file for failed invoice processing
    /// </summary>
    /// <param name="errorDetails">Error processing details</param>
    /// <param name="originalFileName">Original XML file name</param>
    /// <param name="connectionId">SFTP connection identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated XML response</returns>
    Task<XmlResponse> GenerateNackResponseAsync(
        ErrorDetails errorDetails, 
        string originalFileName, 
        string connectionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Uploads an XML response to the appropriate SFTP directory
    /// </summary>
    /// <param name="xmlResponse">XML response to upload</param>
    /// <param name="connectionId">SFTP connection identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if upload successful, false otherwise</returns>
    Task<bool> UploadResponseAsync(
        XmlResponse xmlResponse, 
        string connectionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the XML response content before upload
    /// </summary>
    /// <param name="xmlResponse">XML response to validate</param>
    /// <returns>Validation result</returns>
    Task<XmlValidationResult> ValidateResponseAsync(XmlResponse xmlResponse);
    
    /// <summary>
    /// Generates the response file name based on original file and response type
    /// </summary>
    /// <param name="originalFileName">Original XML file name</param>
    /// <param name="responseType">Type of response (ACK/NACK)</param>
    /// <param name="timestamp">Optional timestamp for uniqueness</param>
    /// <returns>Generated response file name</returns>
    string GenerateResponseFileName(
        string originalFileName, 
        XmlResponseType responseType, 
        DateTime? timestamp = null);
    
    /// <summary>
    /// Gets the target directory for the response based on connection and response type
    /// </summary>
    /// <param name="connectionId">SFTP connection identifier</param>
    /// <param name="responseType">Type of response (ACK/NACK)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Target directory path</returns>
    Task<string> GetResponseDirectoryAsync(string connectionId, XmlResponseType responseType, CancellationToken cancellationToken = default);
}