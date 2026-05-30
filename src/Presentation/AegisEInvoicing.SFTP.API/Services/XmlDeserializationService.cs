using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using AegisEInvoicing.ERP.API.Models;
using FirsInvoiceTypes = AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DataAnnotationValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
using ServiceValidationResult = AegisEInvoicing.SFTP.API.Services.Interfaces.ValidationResult;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using ApiCreateInvoiceItemDto = AegisEInvoicing.ERP.API.Models.CreateInvoiceItemDto;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Service for deserializing XML files into CreateInvoiceRequest objects
/// </summary>
public class XmlDeserializationService : IXmlDeserializationService
{
    private readonly ILogger<XmlDeserializationService> _logger;
    private readonly List<string> _supportedSchemaVersions;

    public XmlDeserializationService(ILogger<XmlDeserializationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supportedSchemaVersions = new List<string> { "1.0", "1.1", "2.0" };
    }

    public async Task<CreateInvoiceRequest?> DeserializeInvoiceRequestAsync(
        string xmlContent,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting deserialization of XML file: {FileName}", fileName);

            // Validate XML structure first
            var validationResult = await ValidateXmlStructureAsync(xmlContent, fileName);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("XML structure validation failed for {FileName}: {Errors}",
                    fileName, string.Join(", ", validationResult.Errors));
                return null;
            }

            // Sanitize and parse XML
            var sanitizedXmlContent = SanitizeXmlContent(xmlContent);
            var xmlReaderSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                ValidationType = ValidationType.None,
                CheckCharacters = false
            };

            XDocument xmlDoc;
            using (var stringReader = new StringReader(sanitizedXmlContent))
            using (var xmlReader = XmlReader.Create(stringReader, xmlReaderSettings))
            {
                xmlDoc = XDocument.Load(xmlReader);
            }
            var rootElement = xmlDoc.Root;

            if (rootElement == null)
            {
                _logger.LogError("No root element found in XML file: {FileName}", fileName);
                return null;
            }

            // Extract business ID first
            var businessId = ExtractBusinessIdFromXml(xmlContent);
            if (businessId == null || businessId == Guid.Empty)
            {
                _logger.LogError("Business ID not found or invalid in XML file: {FileName}", fileName);
                return null;
            }

            // Deserialize based on root element name
            var invoiceRequest = rootElement.Name.LocalName.ToLowerInvariant() switch
            {
                "invoice" => await DeserializeInvoiceElementAsync(rootElement, businessId.Value, fileName),
                "createinvoicerequest" => await DeserializeCreateInvoiceRequestElementAsync(rootElement, businessId.Value, fileName),
                _ => await DeserializeGenericInvoiceAsync(rootElement, businessId.Value, fileName)
            };

            if (invoiceRequest != null)
            {
                // Validate business rules
                var businessValidation = await ValidateInvoiceRequestAsync(invoiceRequest, fileName);
                if (!businessValidation.IsValid)
                {
                    _logger.LogWarning("Business rule validation failed for {FileName}: {Errors}",
                        fileName, JsonSerializer.Serialize(businessValidation.Errors));
                    return null;
                }

                _logger.LogDebug("Successfully deserialized XML file: {FileName}", fileName);
            }

            return invoiceRequest;
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "XML parsing error for file {FileName}: {Message}", fileName, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deserializing XML file {FileName}: {Message}", fileName, ex.Message);
            return null;
        }
    }

    public async Task<XmlValidationResult> ValidateXmlStructureAsync(string xmlContent, string fileName)
    {
        try
        {
            // Sanitize XML content first
            var sanitizedXmlContent = SanitizeXmlContent(xmlContent);

            // Basic XML parsing validation with better settings
            var xmlReaderSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                ValidationType = ValidationType.None,
                CheckCharacters = false  // Allow problematic characters
            };

            XDocument xmlDoc;
            using (var stringReader = new StringReader(sanitizedXmlContent))
            using (var xmlReader = XmlReader.Create(stringReader, xmlReaderSettings))
            {
                xmlDoc = XDocument.Load(xmlReader);
            }

            if (xmlDoc.Root == null)
            {
                return XmlValidationResult.Failure("XML document has no root element");
            }

            var warnings = new List<string>();
            var errors = new List<string>();

            // Check for required elements structure based on the expected XML format
            var requiredElements = new[] { "AegisBusinessId", "party", "invoiceItems" };
            foreach (var requiredElement in requiredElements)
            {
                if (!HasElementRecursive(xmlDoc.Root, requiredElement))
                {
                    errors.Add($"Required element '{requiredElement}' not found in XML");
                }
            }

            // Extract schema version and document type
            var schemaVersion = ExtractSchemaVersion(xmlDoc);
            var documentType = xmlDoc.Root.Name.LocalName;

            // Validate schema version
            if (!string.IsNullOrEmpty(schemaVersion) && !_supportedSchemaVersions.Contains(schemaVersion))
            {
                warnings.Add($"Schema version '{schemaVersion}' may not be fully supported. Supported versions: {string.Join(", ", _supportedSchemaVersions)}");
            }

            var result = errors.Count == 0
                ? XmlValidationResult.Success(schemaVersion, documentType)
                : XmlValidationResult.Failure(errors);

            result.Warnings = warnings;

            _logger.LogDebug("XML structure validation completed for {FileName}. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                fileName, result.IsValid, errors.Count, warnings.Count);

            return result;
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "XML validation error for file {FileName}: {Message}", fileName, ex.Message);
            return XmlValidationResult.Failure($"Invalid XML format: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating XML file {FileName}: {Message}", fileName, ex.Message);
            return XmlValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    public async Task<ServiceValidationResult> ValidateInvoiceRequestAsync(CreateInvoiceRequest invoiceRequest, string fileName)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        try
        {
            // Use data annotations validation
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(invoiceRequest);
            var dataAnnotationResults = new List<DataAnnotationValidationResult>();

            if (!ValidationExtensions.TryValidateObjectRecursively(invoiceRequest, validationContext, dataAnnotationResults))
            {
                foreach (var result in dataAnnotationResults)
                {
                    var field = result.MemberNames.FirstOrDefault() ?? "Unknown";
                    errors.Add(new ValidationError(field, result.ErrorMessage ?? "Validation failed", "VALIDATION_ERROR"));
                }
            }

            // Custom business rule validations
            ValidateBusinessRules(invoiceRequest, errors, warnings);

            var validationResult = errors.Count == 0
                ? ServiceValidationResult.Success()
                : ServiceValidationResult.Failure(errors);

            validationResult.Warnings = warnings;

            _logger.LogDebug("Business rule validation completed for {FileName}. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                fileName, validationResult.IsValid, errors.Count, warnings.Count);

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invoice request from file {FileName}: {Message}", fileName, ex.Message);
            return ServiceValidationResult.Failure(new ValidationError("General", $"Validation error: {ex.Message}", "VALIDATION_EXCEPTION"));
        }
    }

    public Guid? ExtractBusinessIdFromXml(string xmlContent)
    {
        try
        {
            var sanitizedXmlContent = SanitizeXmlContent(xmlContent);
            var xmlDoc = XDocument.Parse(sanitizedXmlContent);

            // Try different possible locations for AegisBusinessId (preferred) or BusinessId (backward compatibility)
            var businessIdElement = xmlDoc.Descendants()
                .FirstOrDefault(e =>
                    e.Name.LocalName.Equals("AegisBusinessId", StringComparison.OrdinalIgnoreCase) ||
                    e.Name.LocalName.Equals("BusinessId", StringComparison.OrdinalIgnoreCase));

            if (businessIdElement != null && Guid.TryParse(businessIdElement.Value, out var businessId))
            {
                return businessId;
            }

            // Try as attribute
            var businessIdAttribute = xmlDoc.Descendants()
                .SelectMany(e => e.Attributes())
                .FirstOrDefault(a =>
                    a.Name.LocalName.Equals("AegisBusinessId", StringComparison.OrdinalIgnoreCase) ||
                    a.Name.LocalName.Equals("BusinessId", StringComparison.OrdinalIgnoreCase));

            if (businessIdAttribute != null && Guid.TryParse(businessIdAttribute.Value, out var businessIdFromAttr))
            {
                return businessIdFromAttr;
            }

            _logger.LogWarning("AegisBusinessId/BusinessId not found in XML content");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting AegisBusinessId from XML: {Message}", ex.Message);
            return null;
        }
    }

    public List<string> GetSupportedSchemaVersions()
    {
        return _supportedSchemaVersions.ToList();
    }

    #region Private Methods

    private async Task<CreateInvoiceRequest?> DeserializeInvoiceElementAsync(XElement invoiceElement, Guid businessId, string fileName)
    {
        try
        {
            return new CreateInvoiceRequest
            {
                AegisBusinessId = businessId,
                IssueDate = ParseDateOnly(invoiceElement, "IssueDate") ?? DateOnly.FromDateTime(DateTime.Today),
                DueDate = ParseDateOnly(invoiceElement, "DueDate"),
                IssueTime = ParseTimeOnly(invoiceElement, "IssueTime"),
                InvoiceType = ParseInvoiceType(invoiceElement),
                Currency = ParseCurrency(invoiceElement),
                DeliveryPeriod = ParseDeliveryPeriod(invoiceElement),
                PaymentMeans = ParsePaymentMeans(invoiceElement),
                Note = GetElementValue(invoiceElement, "Note"),
                PaymentReference = GetElementValue(invoiceElement, "PaymentReference"),
                PaymentTerms = GetElementValue(invoiceElement, "PaymentTerms"),
                Party = ParseParty(invoiceElement),
                InvoiceItems = ParseInvoiceItems(invoiceElement),
                BillingReferences = ParseBillingReferences(invoiceElement),
                DispatchDocumentReference = ParseDocumentReference(invoiceElement, "DispatchDocumentReference"),
                ReceiptDocumentReference = ParseDocumentReference(invoiceElement, "ReceiptDocumentReference"),
                OriginatorDocumentReference = ParseDocumentReference(invoiceElement, "OriginatorDocumentReference"),
                ContractDocumentReference = ParseDocumentReference(invoiceElement, "ContractDocumentReference"),
                AdditionalDocumentReferences = ParseAdditionalDocumentReferences(invoiceElement)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing invoice element from {FileName}: {Message}", fileName, ex.Message);
            return null;
        }
    }

    private async Task<CreateInvoiceRequest?> DeserializeCreateInvoiceRequestElementAsync(XElement requestElement, Guid businessId, string fileName)
    {
        // Similar to DeserializeInvoiceElementAsync but for CreateInvoiceRequest root element
        return await DeserializeInvoiceElementAsync(requestElement, businessId, fileName);
    }

    private async Task<CreateInvoiceRequest?> DeserializeGenericInvoiceAsync(XElement rootElement, Guid businessId, string fileName)
    {
        // Generic deserialization logic for unknown root elements
        return await DeserializeInvoiceElementAsync(rootElement, businessId, fileName);
    }

    private InvoiceTypeRequest ParseInvoiceType(XElement parentElement)
    {
        var invoiceTypeElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("InvoiceType", StringComparison.OrdinalIgnoreCase));
        if (invoiceTypeElement == null)
        {
            // Default invoice type
            return new InvoiceTypeRequest { Name = "Standard Invoice", Code = 1 };
        }

        return new InvoiceTypeRequest
        {
            Name = GetElementValue(invoiceTypeElement, "Name") ?? "Standard Invoice",
            Code = int.TryParse(GetElementValue(invoiceTypeElement, "Code"), out var code) ? code : 1
        };
    }

    private CurrencyRequest ParseCurrency(XElement parentElement)
    {
        var currencyElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("Currency", StringComparison.OrdinalIgnoreCase));
        if (currencyElement == null)
        {
            // Default currency
            return new CurrencyRequest { Name = "Nigerian Naira", Code = "NGN" };
        }

        return new CurrencyRequest
        {
            Name = GetElementValue(currencyElement, "Name") ?? "Nigerian Naira",
            Code = GetElementValue(currencyElement, "Code") ?? "NGN"
        };
    }

    private DeliveryPeriodRequest ParseDeliveryPeriod(XElement parentElement)
    {
        var deliveryElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("DeliveryPeriod", StringComparison.OrdinalIgnoreCase));
        var issueDate = ParseDateOnly(parentElement, "IssueDate") ?? DateOnly.FromDateTime(DateTime.Today);

        if (deliveryElement == null)
        {
            return new DeliveryPeriodRequest
            {
                StartDate = issueDate,
                EndDate = issueDate.AddDays(30)
            };
        }

        return new DeliveryPeriodRequest
        {
            StartDate = ParseDateOnly(deliveryElement, "StartDate") ?? issueDate,
            EndDate = ParseDateOnly(deliveryElement, "EndDate") ?? issueDate.AddDays(30)
        };
    }

    private PaymentMeansRequest ParsePaymentMeans(XElement parentElement)
    {
        var paymentElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("PaymentMeans", StringComparison.OrdinalIgnoreCase));
        if (paymentElement == null)
        {
            return new PaymentMeansRequest { Code = "01", Name = "Cash" };
        }

        return new PaymentMeansRequest
        {
            Code = GetElementValue(paymentElement, "Code") ?? "01",
            Name = GetElementValue(paymentElement, "Name") ?? "Cash"
        };
    }

    private PartyRequest ParseParty(XElement parentElement)
    {
        var partyElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("Party", StringComparison.OrdinalIgnoreCase));
        if (partyElement == null)
        {
            throw new InvalidOperationException("Party information is required but not found in XML");
        }

        return new PartyRequest
        {
            Name = GetElementValue(partyElement, "Name") ?? throw new InvalidOperationException("Party name is required"),
            Description = GetElementValue(partyElement, "Description") ?? "Invoice party",
            Phone = GetElementValue(partyElement, "Phone") ?? throw new InvalidOperationException("Party phone is required"),
            Email = GetElementValue(partyElement, "Email") ?? throw new InvalidOperationException("Party email is required"),
            TaxIdentificationNumber = GetElementValue(partyElement, "TaxIdentificationNumber") ?? throw new InvalidOperationException("Party TIN is required"),
            Address = ParseAddress(partyElement)
        };
    }

    private AddressRequest ParseAddress(XElement parentElement)
    {
        var addressElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("Address", StringComparison.OrdinalIgnoreCase));
        if (addressElement == null)
        {
            throw new InvalidOperationException("Address information is required but not found in XML");
        }

        return new AddressRequest
        {
            Street = GetElementValue(addressElement, "Street") ?? throw new InvalidOperationException("Street address is required"),
            City = GetElementValue(addressElement, "City") ?? throw new InvalidOperationException("City is required"),
            State = GetElementValue(addressElement, "State") ?? throw new InvalidOperationException("State is required"),
            Country = GetElementValue(addressElement, "Country") ?? "Nigeria",
            PostalCode = GetElementValue(addressElement, "PostalCode")
        };
    }

    private List<ApiCreateInvoiceItemDto> ParseInvoiceItems(XElement parentElement)
    {
        var items = new List<ApiCreateInvoiceItemDto>();

        // First try to find invoiceItems container, then look for item elements inside it
        var invoiceItemsContainer = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("InvoiceItems", StringComparison.OrdinalIgnoreCase));
        List<XElement> itemElements;

        if (invoiceItemsContainer != null)
        {
            // Look for item elements within invoiceItems container
            itemElements = invoiceItemsContainer.Descendants()
                .Where(e => e.Name.LocalName.Equals("Item", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            // Fallback: try to find items directly
            itemElements = parentElement.Descendants()
                .Where(e => e.Name.LocalName.Equals("InvoiceItem", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!itemElements.Any())
            {
                itemElements = parentElement.Descendants()
                    .Where(e => e.Name.LocalName.Equals("Item", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        foreach (var itemElement in itemElements)
        {
            try
            {
                var item = new ApiCreateInvoiceItemDto
                {
                    Name = GetElementValue(itemElement, "Name") ?? "Invoice Item",
                    ItemDescription = GetElementValue(itemElement, "Description") ?? GetElementValue(itemElement, "ItemDescription") ?? "Invoice item description",
                    UnitPrice = decimal.TryParse(GetElementValue(itemElement, "UnitPrice"), out var price) ? price : 0,
                    Quantity = int.TryParse(GetElementValue(itemElement, "Quantity"), out var qty) ? qty : 1,
                    ServiceCode = ParseServiceCode(itemElement),
                    TaxCategories = [ParseTaxCategory(itemElement)],
                    DiscountFee = ParseDiscountFee(itemElement),
                    AdditionalFee = ParseAdditionalFee(itemElement)
                };

                items.Add(item);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing invoice item, skipping: {Message}", ex.Message);
            }
        }

        return items;
    }

    private List<FirsInvoiceTypes.BillingReferenceRequest>? ParseBillingReferences(XElement parentElement)
    {
        var container = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("BillingReferences", StringComparison.OrdinalIgnoreCase));

        if (container == null)
        {
            return null;
        }

        var references = new List<FirsInvoiceTypes.BillingReferenceRequest>();

        var referenceElements = container.Descendants()
            .Where(e => e.Name.LocalName.Equals("BillingReference", StringComparison.OrdinalIgnoreCase));

        foreach (var referenceElement in referenceElements)
        {
            try
            {
                var irn = GetElementValue(referenceElement, "Irn");
                var issueDate = ParseDateOnly(referenceElement, "IssueDate");

                if (string.IsNullOrWhiteSpace(irn) || issueDate is null)
                {
                    _logger.LogWarning("Skipping billingReference element due to missing/invalid IRN or IssueDate");
                    continue;
                }

                references.Add(new FirsInvoiceTypes.BillingReferenceRequest
                {
                    Irn = irn,
                    IssueDate = issueDate.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing billing reference, skipping: {Message}", ex.Message);
            }
        }

        return references.Count > 0 ? references : null;
    }

    private FirsInvoiceTypes.DocumentReferenceRequest? ParseDocumentReference(XElement parentElement, string elementName)
    {
        var docElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));

        if (docElement == null)
        {
            return null;
        }

        try
        {
            var irn = GetElementValue(docElement, "Irn");
            var issueDate = ParseDateOnly(docElement, "IssueDate");

            if (string.IsNullOrWhiteSpace(irn) || issueDate is null)
            {
                _logger.LogWarning("Skipping {ElementName} element due to missing/invalid IRN or IssueDate", elementName);
                return null;
            }

            return new FirsInvoiceTypes.DocumentReferenceRequest
            {
                Irn = irn,
                IssueDate = issueDate.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing {ElementName} document reference, skipping: {Message}", elementName, ex.Message);
            return null;
        }
    }

    private List<FirsInvoiceTypes.DocumentReferenceRequest>? ParseAdditionalDocumentReferences(XElement parentElement)
    {
        var container = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("AdditionalDocumentReferences", StringComparison.OrdinalIgnoreCase));

        if (container == null)
        {
            return null;
        }

        var references = new List<FirsInvoiceTypes.DocumentReferenceRequest>();

        var referenceElements = container.Descendants()
            .Where(e => e.Name.LocalName.Equals("AdditionalDocumentReference", StringComparison.OrdinalIgnoreCase));

        foreach (var referenceElement in referenceElements)
        {
            try
            {
                var irn = GetElementValue(referenceElement, "Irn");
                var issueDate = ParseDateOnly(referenceElement, "IssueDate");

                if (string.IsNullOrWhiteSpace(irn) || issueDate is null)
                {
                    _logger.LogWarning("Skipping additionalDocumentReference element due to missing/invalid IRN or IssueDate");
                    continue;
                }

                references.Add(new FirsInvoiceTypes.DocumentReferenceRequest
                {
                    Irn = irn,
                    IssueDate = issueDate.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing additional document reference, skipping: {Message}", ex.Message);
            }
        }

        return references.Count > 0 ? references : null;
    }

    private ServiceCodeRequest ParseServiceCode(XElement parentElement)
    {
        var serviceElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("ServiceCode", StringComparison.OrdinalIgnoreCase));

        return new ServiceCodeRequest
        {
            Code = GetElementValue(serviceElement, "Code") ?? "9999",
            Name = GetElementValue(serviceElement, "Name") ?? "General Service"
        };
    }

    private TaxCategoryRequest ParseTaxCategory(XElement parentElement)
    {
        var taxElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("TaxCategory", StringComparison.OrdinalIgnoreCase));

        return new TaxCategoryRequest
        {
            Name = GetElementValue(taxElement, "Name") ?? "Standard Rate",
            IsPercentage = true,
            Percent = decimal.TryParse(GetElementValue(taxElement, "Percent"), out var percent) ? percent : 7.5m
        };
    }

    private DiscountFeeDto? ParseDiscountFee(XElement parentElement)
    {
        var discountElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("DiscountFee", StringComparison.OrdinalIgnoreCase));
        if (discountElement == null) return null;

        return new DiscountFeeDto
        {
            Amount = decimal.TryParse(GetElementValue(discountElement, "Amount"), out var amount) ? amount : 0,
            Code = ParseFeeStandardUnit(GetElementValue(discountElement, "Code"))
        };
    }

    private AdditionalFeeDto? ParseAdditionalFee(XElement parentElement)
    {
        var feeElement = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("AdditionalFee", StringComparison.OrdinalIgnoreCase));
        if (feeElement == null) return null;

        return new AdditionalFeeDto
        {
            Amount = decimal.TryParse(GetElementValue(feeElement, "Amount"), out var amount) ? amount : 0,
            Code = ParseFeeStandardUnit(GetElementValue(feeElement, "Code"))
        };
    }

    private DateOnly? ParseDateOnly(XElement parentElement, string elementName)
    {
        var value = GetElementValue(parentElement, elementName);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(value, out var date))
            return DateOnly.FromDateTime(date);

        if (DateOnly.TryParse(value, out var dateOnly))
            return dateOnly;

        return null;
    }

    private TimeOnly? ParseTimeOnly(XElement parentElement, string elementName)
    {
        var value = GetElementValue(parentElement, elementName);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (TimeOnly.TryParse(value, out var timeOnly))
            return timeOnly;

        // Try parsing as DateTime and extract time part
        if (DateTime.TryParse(value, out var dateTime))
            return TimeOnly.FromDateTime(dateTime);

        return null;
    }

    private AegisEInvoicing.Domain.Enums.FeeStandardUnit ParseFeeStandardUnit(string? codeValue)
    {
        if (string.IsNullOrWhiteSpace(codeValue))
            return AegisEInvoicing.Domain.Enums.FeeStandardUnit.NGN;

        // Try to parse as enum name first
        if (Enum.TryParse<AegisEInvoicing.Domain.Enums.FeeStandardUnit>(codeValue, true, out var enumValue))
            return enumValue;

        // Try to parse as numeric value
        if (int.TryParse(codeValue, out var numericCode))
        {
            return numericCode switch
            {
                1 => AegisEInvoicing.Domain.Enums.FeeStandardUnit.Percent,
                2 => AegisEInvoicing.Domain.Enums.FeeStandardUnit.NGN,
                _ => AegisEInvoicing.Domain.Enums.FeeStandardUnit.NGN
            };
        }

        return AegisEInvoicing.Domain.Enums.FeeStandardUnit.NGN;
    }

    private string? GetElementValue(XElement? parentElement, string elementName)
    {
        if (parentElement == null) return null;

        var element = parentElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));

        return element?.Value?.Trim();
    }

    private bool HasElementRecursive(XElement element, string elementName)
    {
        return element.Descendants()
            .Any(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));
    }

    private string? ExtractSchemaVersion(XDocument xmlDoc)
    {
        // Try to extract schema version from various locations
        var versionAttribute = xmlDoc.Root?.Attributes()
            .FirstOrDefault(a => a.Name.LocalName.Equals("version", StringComparison.OrdinalIgnoreCase));

        if (versionAttribute != null)
            return versionAttribute.Value;

        var versionElement = xmlDoc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("version", StringComparison.OrdinalIgnoreCase) ||
                                e.Name.LocalName.Equals("schemaversion", StringComparison.OrdinalIgnoreCase));

        return versionElement?.Value?.Trim();
    }

    private void ValidateBusinessRules(CreateInvoiceRequest invoiceRequest, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        // Business rule validations
        if (invoiceRequest.AegisBusinessId == Guid.Empty)
        {
            errors.Add(new ValidationError("AegisBusinessId", "Aegis Business ID cannot be empty", "BUSINESS_ID_REQUIRED"));
        }

        if (invoiceRequest.IssueDate > DateOnly.FromDateTime(DateTime.Today.AddDays(30)))
        {
            warnings.Add(new ValidationWarning("IssueDate", "Issue date is more than 30 days in the future", "FUTURE_DATE_WARNING"));
        }

        if (invoiceRequest.InvoiceItems.Count == 0)
        {
            errors.Add(new ValidationError("InvoiceItems", "At least one invoice item is required", "NO_INVOICE_ITEMS"));
        }

        foreach (var item in invoiceRequest.InvoiceItems)
        {
            if (item.UnitPrice <= 0)
            {
                errors.Add(new ValidationError($"InvoiceItems.UnitPrice", "Unit price must be greater than zero", "INVALID_UNIT_PRICE", item.UnitPrice));
            }

            if (item.Quantity <= 0)
            {
                errors.Add(new ValidationError($"InvoiceItems.Quantity", "Quantity must be greater than zero", "INVALID_QUANTITY", item.Quantity));
            }
        }
    }

    /// <summary>
    /// Sanitizes XML content to handle problematic characters and entities
    /// </summary>
    /// <param name="xmlContent">The raw XML content</param>
    /// <returns>Sanitized XML content</returns>
    private string SanitizeXmlContent(string xmlContent)
    {
        if (string.IsNullOrEmpty(xmlContent))
            return xmlContent;

        try
        {
            _logger.LogDebug("Sanitizing XML content");

            // Remove BOM if present
            if (xmlContent.Length > 0 && xmlContent[0] == '\uFEFF')
            {
                xmlContent = xmlContent.Substring(1);
                _logger.LogDebug("Removed BOM from XML content");
            }

            // Remove null characters
            xmlContent = xmlContent.Replace("\0", "");

            // Fix common XML entity issues
            var sanitized = xmlContent;

            // Fix unescaped ampersands that are not part of valid entities
            // This regex finds & that are not followed by valid entity names
            sanitized = Regex.Replace(sanitized, @"&(?![a-zA-Z][a-zA-Z0-9]*;|#[0-9]+;|#x[0-9a-fA-F]+;)", "&amp;");

            // Fix unescaped less-than and greater-than in text content
            // This is more complex as we need to avoid elements and attributes
            // For now, we'll focus on the most common issues

            // Remove any control characters except tab, newline, and carriage return
            sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");

            // Ensure proper encoding declaration
            if (!sanitized.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                sanitized = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + sanitized;
                _logger.LogDebug("Added XML declaration");
            }
            else
            {
                // Ensure UTF-8 encoding is specified - simplified approach
                if (!sanitized.Contains("encoding", StringComparison.OrdinalIgnoreCase))
                {
                    // Simple string replacement for common XML declaration patterns
                    if (sanitized.Contains("<?xml version=\"1.0\"?>"))
                    {
                        sanitized = sanitized.Replace("<?xml version=\"1.0\"?>", "<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        _logger.LogDebug("Added UTF-8 encoding to XML declaration");
                    }
                    else if (sanitized.Contains("<?xml version='1.0'?>"))
                    {
                        sanitized = sanitized.Replace("<?xml version='1.0'?>", "<?xml version='1.0' encoding='UTF-8'?>");
                        _logger.LogDebug("Added UTF-8 encoding to XML declaration");
                    }
                }
            }

            _logger.LogDebug("XML content sanitization completed");
            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during XML sanitization, returning original content");
            return xmlContent; // Return original if sanitization fails
        }
    }

    #endregion
}

/// <summary>
/// Extension methods for validation
/// </summary>
public static class ValidationExtensions
{
    public static bool TryValidateObjectRecursively<T>(T obj, System.ComponentModel.DataAnnotations.ValidationContext validationContext, ICollection<DataAnnotationValidationResult> results)
    {
        return obj is not null && Validator.TryValidateObject(obj, validationContext, results, true);
    }
}