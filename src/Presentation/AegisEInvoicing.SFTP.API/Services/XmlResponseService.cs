using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using System.Xml;
using System.Xml.Linq;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Service for generating and managing XML response files (ACK/NACK)
/// </summary>
public class XmlResponseService(
    ISftpService sftpService,
    ILogger<XmlResponseService> logger) : IXmlResponseService
{
    private readonly ISftpService _sftpService = sftpService ?? throw new ArgumentNullException(nameof(sftpService));
    private readonly ILogger<XmlResponseService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<XmlResponse> GenerateAckResponseAsync(
        InvoiceDetails invoiceDetails, 
        string originalFileName, 
        string connectionId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating ACK response for file {FileName} on connection {ConnectionId}",
                originalFileName, connectionId);

            var responseFileName = GenerateResponseFileName(originalFileName, XmlResponseType.ACK);
            var targetDirectory = await GetResponseDirectoryAsync(connectionId, XmlResponseType.ACK, cancellationToken).ConfigureAwait(false);

            var xmlContent = GenerateAckXmlContent(invoiceDetails, originalFileName);

            var response = new XmlResponse
            {
                FileName = responseFileName,
                Content = xmlContent,
                Type = XmlResponseType.ACK,
                TargetDirectory = targetDirectory,
                Invoice = invoiceDetails,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Successfully generated ACK response for {FileName}. Response file: {ResponseFileName}",
                originalFileName, responseFileName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ACK response for file {FileName} on connection {ConnectionId}: {Message}",
                originalFileName, connectionId, ex.Message);
            throw;
        }
    }

    public async Task<XmlResponse> GenerateNackResponseAsync(
        ErrorDetails errorDetails, 
        string originalFileName, 
        string connectionId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating NACK response for file {FileName} on connection {ConnectionId}",
                originalFileName, connectionId);

            var responseFileName = GenerateResponseFileName(originalFileName, XmlResponseType.NACK);
            var targetDirectory = await GetResponseDirectoryAsync(connectionId, XmlResponseType.NACK, cancellationToken).ConfigureAwait(false);

            var xmlContent = GenerateNackXmlContent(errorDetails, originalFileName);

            var response = new XmlResponse
            {
                FileName = responseFileName,
                Content = xmlContent,
                Type = XmlResponseType.NACK,
                TargetDirectory = targetDirectory,
                Error = errorDetails,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Successfully generated NACK response for {FileName}. Response file: {ResponseFileName}",
                originalFileName, responseFileName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating NACK response for file {FileName} on connection {ConnectionId}: {Message}",
                originalFileName, connectionId, ex.Message);
            throw;
        }
    }

    public async Task<bool> UploadResponseAsync(
        XmlResponse xmlResponse, 
        string connectionId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Uploading {ResponseType} response file {FileName} to connection {ConnectionId}",
                xmlResponse.Type, xmlResponse.FileName, connectionId);

            // Validate response before upload
            var validationResult = await ValidateResponseAsync(xmlResponse);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Response validation failed for {FileName}: {Errors}",
                    xmlResponse.FileName, string.Join(", ", validationResult.Errors));
                return false;
            }

            // Ensure target directory exists
            await _sftpService.CreateDirectoryIfNotExistsAsync(connectionId, xmlResponse.TargetDirectory, cancellationToken);

            // Upload the file
            var remoteFilePath = Path.Combine(xmlResponse.TargetDirectory, xmlResponse.FileName).Replace('\\', '/');
            await _sftpService.UploadFileAsync(connectionId, remoteFilePath, xmlResponse.Content, cancellationToken);

            _logger.LogInformation("Successfully uploaded {ResponseType} response file {FileName} to {RemotePath}",
                xmlResponse.Type, xmlResponse.FileName, remoteFilePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading {ResponseType} response file {FileName} to connection {ConnectionId}: {Message}",
                xmlResponse.Type, xmlResponse.FileName, connectionId, ex.Message);
            return false;
        }
    }

    public async Task<XmlValidationResult> ValidateResponseAsync(XmlResponse xmlResponse)
    {
        try
        {
            if (xmlResponse == null)
            {
                return XmlValidationResult.Failure("XML response is null");
            }

            if (string.IsNullOrWhiteSpace(xmlResponse.Content))
            {
                return XmlValidationResult.Failure("XML response content is empty");
            }

            if (string.IsNullOrWhiteSpace(xmlResponse.FileName))
            {
                return XmlValidationResult.Failure("XML response file name is empty");
            }

            if (string.IsNullOrWhiteSpace(xmlResponse.TargetDirectory))
            {
                return XmlValidationResult.Failure("XML response target directory is empty");
            }

            // Validate XML structure
            try
            {
                var xmlDoc = XDocument.Parse(xmlResponse.Content);
                if (xmlDoc.Root == null)
                {
                    return XmlValidationResult.Failure("XML response has no root element");
                }
            }
            catch (XmlException ex)
            {
                return XmlValidationResult.Failure($"Invalid XML structure: {ex.Message}");
            }

            // Validate response type specific content
            var contentValidation = xmlResponse.Type switch
            {
                XmlResponseType.ACK => ValidateAckContent(xmlResponse),
                XmlResponseType.NACK => ValidateNackContent(xmlResponse),
                _ => XmlValidationResult.Failure($"Unknown response type: {xmlResponse.Type}")
            };

            if (!contentValidation.IsValid)
            {
                return contentValidation;
            }

            _logger.LogDebug("Response validation passed for {FileName}", xmlResponse.FileName);
            return XmlValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating XML response: {Message}", ex.Message);
            return XmlValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    public string GenerateResponseFileName(
        string originalFileName, 
        XmlResponseType responseType, 
        DateTime? timestamp = null)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var responseTimestamp = timestamp ?? DateTime.UtcNow;
        var timestampString = responseTimestamp.ToString("yyyyMMdd_HHmmss");
        
        return $"{fileNameWithoutExtension}_{responseType}_{timestampString}.xml";
    }

    public async Task<string> GetResponseDirectoryAsync(string connectionId, XmlResponseType responseType, CancellationToken cancellationToken = default)
    {
        var connectionDetails = await _sftpService.GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        var responseDir = responseType switch
        {
            XmlResponseType.ACK => connectionDetails.ReceiptsDirectory,
            XmlResponseType.NACK => connectionDetails.RejectedDirectory,
            _ => throw new ArgumentException($"Unknown response type: {responseType}", nameof(responseType))
        };

        // Get parent directory of WorkingDirectory (e.g., /uploads/{businessId}/In-Progress -> /uploads/{businessId})
        var baseDirectory = Path.GetDirectoryName(connectionDetails.WorkingDirectory)?.Replace('\\', '/') ?? "/";

        // Combine with base directory if response directory is relative
        if (!Path.IsPathRooted(responseDir))
        {
            responseDir = Path.Combine(baseDirectory, responseDir).Replace('\\', '/');
        }

        return responseDir;
    }

    #region Private Methods

    private string GenerateAckXmlContent(InvoiceDetails invoiceDetails, string originalFileName)
    {
        try
        {
            var ackNamespace = XNamespace.Get("http://aegiseinvoicing.com/schemas/ack");
            var xmlDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ackNamespace + "AcknowledgmentResponse",
                    new XAttribute("version", "1.0"),
                    new XAttribute("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                    
                    new XElement(ackNamespace + "ResponseHeader",
                        new XElement(ackNamespace + "MessageId", Guid.NewGuid().ToString()),
                        new XElement(ackNamespace + "OriginalFileName", originalFileName),
                        new XElement(ackNamespace + "ProcessedAt", invoiceDetails.ProcessedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                        new XElement(ackNamespace + "Status", "SUCCESS"),
                        new XElement(ackNamespace + "Message", "Invoice processed successfully")
                    ),
                    
                    new XElement(ackNamespace + "InvoiceInfo",
                        new XElement(ackNamespace + "InvoiceId", invoiceDetails.InvoiceId.ToString()),
                        new XElement(ackNamespace + "PartyId", invoiceDetails.PartyId.ToString()),
                        new XElement(ackNamespace + "IRN", invoiceDetails.IRN),
                        new XElement(ackNamespace + "BusinessId", invoiceDetails.BusinessId),
                        invoiceDetails.InvoiceNumber != null ? new XElement(ackNamespace + "InvoiceNumber", invoiceDetails.InvoiceNumber) : null,
                        invoiceDetails.IssueDate.HasValue ? new XElement(ackNamespace + "IssueDate", invoiceDetails.IssueDate.Value.ToString("yyyy-MM-dd")) : null,
                        invoiceDetails.TotalAmount.HasValue ? new XElement(ackNamespace + "TotalAmount", invoiceDetails.TotalAmount.Value.ToString("F2")) : null,
                        invoiceDetails.Currency != null ? new XElement(ackNamespace + "Currency", invoiceDetails.Currency) : null
                    ),
                    
                    new XElement(ackNamespace + "ProcessingInfo",
                        new XElement(ackNamespace + "ProcessingInstance", Environment.MachineName),
                        new XElement(ackNamespace + "ProcessingVersion", "1.0"),
                        new XElement(ackNamespace + "NextSteps", "Invoice has been successfully recorded and is submited to FIRS")
                    )
                )
            );

            return xmlDoc.ToString();
        }
        catch (Exception)
        {
            // Fallback to simple XML generation if namespace approach fails
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<AcknowledgmentResponse version=""1.0"" timestamp=""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}"">
  <ResponseHeader>
    <MessageId>{Guid.NewGuid()}</MessageId>
    <OriginalFileName>{System.Security.SecurityElement.Escape(originalFileName)}</OriginalFileName>
    <ProcessedAt>{invoiceDetails.ProcessedAt:yyyy-MM-ddTHH:mm:ss.fffZ}</ProcessedAt>
    <Status>SUCCESS</Status>
    <Message>Invoice processed successfully</Message>
  </ResponseHeader>
  <InvoiceInfo>
    <InvoiceId>{invoiceDetails.InvoiceId}</InvoiceId>
    <PartyId>{invoiceDetails.PartyId}</PartyId>
    <IRN>{System.Security.SecurityElement.Escape(invoiceDetails.IRN)}</IRN>
    <BusinessId>{System.Security.SecurityElement.Escape(invoiceDetails.BusinessId)}</BusinessId>
  </InvoiceInfo>
</AcknowledgmentResponse>";
        }
    }

    private string GenerateNackXmlContent(ErrorDetails errorDetails, string originalFileName)
    {
        try
        {
            var nackNamespace = XNamespace.Get("http://aegiseinvoicing.com/schemas/nack");
            var xmlDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(nackNamespace + "NegativeAcknowledgmentResponse",
                    new XAttribute("version", "1.0"),
                    new XAttribute("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                    
                    new XElement(nackNamespace + "ResponseHeader",
                        new XElement(nackNamespace + "MessageId", Guid.NewGuid().ToString()),
                        new XElement(nackNamespace + "OriginalFileName", originalFileName),
                        new XElement(nackNamespace + "ProcessedAt", errorDetails.ErrorOccurredAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                        new XElement(nackNamespace + "Status", "ERROR"),
                        new XElement(nackNamespace + "Message", string.IsNullOrWhiteSpace(errorDetails.ErrorMessage)
                            ? "Invoice processing failed"
                            : errorDetails.ErrorMessage)
                    ),
                    
                    new XElement(nackNamespace + "ErrorInfo",
                        new XElement(nackNamespace + "ErrorCode", errorDetails.ErrorCode),
                        new XElement(nackNamespace + "ErrorMessage", errorDetails.ErrorMessage),
                        new XElement(nackNamespace + "ErrorSeverity", errorDetails.ErrorSeverity),
                        new XElement(nackNamespace + "ErrorTimestamp", errorDetails.ErrorOccurredAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                        !string.IsNullOrWhiteSpace(errorDetails.ValidationErrors) 
                            ? new XElement(nackNamespace + "ValidationErrors", new XCData(errorDetails.ValidationErrors))
                            : null,
                        !string.IsNullOrWhiteSpace(errorDetails.StackTrace) 
                            ? new XElement(nackNamespace + "TechnicalDetails", new XCData(errorDetails.StackTrace))
                            : null
                    ),
                    
                    new XElement(nackNamespace + "ProcessingInfo",
                        new XElement(nackNamespace + "ProcessingInstance", Environment.MachineName),
                        new XElement(nackNamespace + "ProcessingVersion", "1.0"),
                        new XElement(nackNamespace + "RecommendedAction", GetRecommendedActionForError(errorDetails.ErrorCode)),
                        new XElement(nackNamespace + "SupportContact", "support@aegiseinvoicing.com")
                    )
                )
            );

            return xmlDoc.ToString();
        }
        catch (Exception)
        {
            // Fallback to simple XML generation if namespace approach fails
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<NegativeAcknowledgmentResponse version=""1.0"" timestamp=""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}"">
  <ResponseHeader>
    <MessageId>{Guid.NewGuid()}</MessageId>
    <OriginalFileName>{System.Security.SecurityElement.Escape(originalFileName)}</OriginalFileName>
    <ProcessedAt>{errorDetails.ErrorOccurredAt:yyyy-MM-ddTHH:mm:ss.fffZ}</ProcessedAt>
    <Status>ERROR</Status>
    <Message>{System.Security.SecurityElement.Escape(string.IsNullOrWhiteSpace(errorDetails.ErrorMessage) ? "Invoice processing failed" : errorDetails.ErrorMessage)}</Message>
  </ResponseHeader>
  <ErrorInfo>
    <ErrorCode>{System.Security.SecurityElement.Escape(errorDetails.ErrorCode)}</ErrorCode>
    <ErrorMessage>{System.Security.SecurityElement.Escape(errorDetails.ErrorMessage)}</ErrorMessage>
    <ErrorSeverity>{System.Security.SecurityElement.Escape(errorDetails.ErrorSeverity)}</ErrorSeverity>
    <ErrorTimestamp>{errorDetails.ErrorOccurredAt:yyyy-MM-ddTHH:mm:ss.fffZ}</ErrorTimestamp>
  </ErrorInfo>
  <ProcessingInfo>
    <ProcessingInstance>{System.Security.SecurityElement.Escape(Environment.MachineName)}</ProcessingInstance>
    <ProcessingVersion>1.0</ProcessingVersion>
    <RecommendedAction>{System.Security.SecurityElement.Escape(GetRecommendedActionForError(errorDetails.ErrorCode))}</RecommendedAction>
    <SupportContact>support@aegiseinvoicing.com</SupportContact>
  </ProcessingInfo>
</NegativeAcknowledgmentResponse>";
        }
    }

    private XmlValidationResult ValidateAckContent(XmlResponse xmlResponse)
    {
        try
        {
            var xmlDoc = XDocument.Parse(xmlResponse.Content);
            var errors = new List<string>();

            // Check required elements for ACK (handle both namespaced and non-namespaced)
            var requiredElements = new[] { "ResponseHeader", "InvoiceInfo", "ProcessingInfo" };
            foreach (var element in requiredElements)
            {
                var foundElement = xmlDoc.Descendants()
                    .Any(e => e.Name.LocalName.Equals(element, StringComparison.OrdinalIgnoreCase));
                if (!foundElement)
                {
                    errors.Add($"Required element '{element}' not found in ACK response");
                }
            }

            // Check specific ACK fields (handle both namespaced and non-namespaced)
            if (xmlResponse.Invoice != null)
            {
                var invoiceIdElement = xmlDoc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName.Equals("InvoiceId", StringComparison.OrdinalIgnoreCase));
                if (invoiceIdElement == null || invoiceIdElement.Value != xmlResponse.Invoice.InvoiceId.ToString())
                {
                    errors.Add("InvoiceId mismatch in ACK response");
                }

                var irnElement = xmlDoc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName.Equals("IRN", StringComparison.OrdinalIgnoreCase));
                if (irnElement == null || irnElement.Value != xmlResponse.Invoice.IRN)
                {
                    errors.Add("IRN mismatch in ACK response");
                }
            }

            return errors.Count == 0 
                ? XmlValidationResult.Success() 
                : XmlValidationResult.Failure(errors);
        }
        catch (Exception ex)
        {
            return XmlValidationResult.Failure($"ACK content validation error: {ex.Message}");
        }
    }

    private XmlValidationResult ValidateNackContent(XmlResponse xmlResponse)
    {
        try
        {
            var xmlDoc = XDocument.Parse(xmlResponse.Content);
            var errors = new List<string>();

            // Check required elements for NACK (handle both namespaced and non-namespaced)
            var requiredElements = new[] { "ResponseHeader", "ErrorInfo", "ProcessingInfo" };
            foreach (var element in requiredElements)
            {
                var foundElement = xmlDoc.Descendants()
                    .Any(e => e.Name.LocalName.Equals(element, StringComparison.OrdinalIgnoreCase));
                if (!foundElement)
                {
                    errors.Add($"Required element '{element}' not found in NACK response");
                }
            }

            // Check specific NACK fields (handle both namespaced and non-namespaced)
            if (xmlResponse.Error != null)
            {
                var errorCodeElement = xmlDoc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName.Equals("ErrorCode", StringComparison.OrdinalIgnoreCase));
                if (errorCodeElement == null || errorCodeElement.Value != xmlResponse.Error.ErrorCode)
                {
                    errors.Add("ErrorCode mismatch in NACK response");
                }

                var errorMessageElement = xmlDoc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName.Equals("ErrorMessage", StringComparison.OrdinalIgnoreCase));
                if (errorMessageElement == null || string.IsNullOrWhiteSpace(errorMessageElement.Value))
                {
                    errors.Add("ErrorMessage is required in NACK response");
                }
            }

            return errors.Count == 0 
                ? XmlValidationResult.Success() 
                : XmlValidationResult.Failure(errors);
        }
        catch (Exception ex)
        {
            return XmlValidationResult.Failure($"NACK content validation error: {ex.Message}");
        }
    }

    private string GetRecommendedActionForError(string errorCode)
    {
        return errorCode switch
        {
            "VALIDATION_ERROR" => "Please correct the validation errors in the XML file and resubmit",
            "BUSINESS_ID_REQUIRED" => "Ensure the Business ID is properly specified in the XML file",
            "NO_INVOICE_ITEMS" => "Add at least one invoice item to the XML file",
            "INVALID_UNIT_PRICE" => "Ensure all unit prices are greater than zero",
            "INVALID_QUANTITY" => "Ensure all quantities are greater than zero",
            "PARSING_ERROR" => "Check the XML file format and ensure it is well-formed",
            "BUSINESS_RULE_VIOLATION" => "Correct the business rule violation and resubmit the invoice",
            _ => "Please review the error details and correct the issues in the XML file before resubmitting"
        };
    }

    #endregion
}