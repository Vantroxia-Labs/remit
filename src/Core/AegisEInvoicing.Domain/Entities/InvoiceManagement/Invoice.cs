using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Events;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

public class Invoice : AuditableAggregateRoot
{
    public string InvoiceCode { get; private set; } = null!;

    public IRN Irn { get; private set; } = null!;
    public DateOnly IssueDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public TimeOnly? IssueTime { get; private set; }
    public InvoiceType InvoiceType { get; private set; } = null!;
    public InvoiceKind? InvoiceKind { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;
    public string? Note { get; private set; }
    public Currency Currency { get; private set; } = null!;

    public DeliveryPeriod DeliveryPeriod { get; private set; } = null!;
    public InvoiceSource InvoiceSource { get; private set; } = InvoiceSource.PORTAL;
    public string? PaymentTerms { get; private set; }
    public string? PaymentReference { get; private set; }
    public PaymentMeans? PaymentMeans { get; private set; }
    private readonly List<InvoiceItem> _invoiceLine = [];
    public IReadOnlyCollection<InvoiceItem> InvoiceLine => _invoiceLine.AsReadOnly();

    private readonly List<InvoiceApprovalHistory> _invoiceApprovalHistory = [];
    public IReadOnlyCollection<InvoiceApprovalHistory> InvoiceApprovalHistory => _invoiceApprovalHistory.AsReadOnly();

    private readonly List<InvoiceBillingReference> _billingReferences = [];
    public IReadOnlyCollection<InvoiceBillingReference> BillingReferences => _billingReferences.AsReadOnly();

    // Document References (all optional)
    public InvoiceDispatchDocumentReference? DispatchDocumentReference { get; private set; }
    public InvoiceReceiptDocumentReference? ReceiptDocumentReference { get; private set; }
    public InvoiceOriginatorDocumentReference? OriginatorDocumentReference { get; private set; }
    public InvoiceContractDocumentReference? ContractDocumentReference { get; private set; }

    private readonly List<InvoiceAdditionalDocumentReference> _additionalDocumentReferences = [];
    public IReadOnlyCollection<InvoiceAdditionalDocumentReference> AdditionalDocumentReferences => _additionalDocumentReferences.AsReadOnly();

    // Environment Mode — tracks whether this invoice was created in Sandbox or Production mode.
    public AppEnvironmentMode EnvironmentMode { get; private set; } = AppEnvironmentMode.Production;

    // FIRS Integration Fields
    public InvoiceStatus InvoiceStatus { get; private set; }
    public QRCode? QRCode { get; private set; }
    public string? FIRSSubmissionId { get; private set; }
    public DateTimeOffset? SubmittedToFIRSAt { get; private set; }
    public string? FIRSSubmissionResponseMessage { get; private set; }


    // Navigation properties
    public Guid BusinessId { get; private set; }
    public Guid PartyId { get; private set; }
    public Business Business { get; private set; } = null!;
    public Party Party { get; private set; } = null!;
    public UserManagement.User CreatedByUser { get; private set; } = null!;

    /// <summary>
    /// Set when this invoice has been captured in a VAT schedule.
    /// Null means the invoice is still eligible to be included in a future schedule.
    /// </summary>
    public Guid? VatScheduleId { get; private set; }
    public VatSchedule? VatSchedule { get; private set; }

    private Invoice() { } // EF Constructor

    #region Factory Methods

    // Existing Create method (kept as is)
    public static Invoice Create(
        Guid businessId,
        Guid partyId,
        IRN irn,
        string invoicePrefix,
        DateOnly issueDate,
        InvoiceType invoiceType,
        Currency currency,
        DeliveryPeriod deliveryPeriod,
        PaymentMeans paymentMeans,
        InvoiceSource invoiceSource,
        InvoiceKind? invoiceKind = null,
        string? note = null,
        string? paymentReference = null,
        string? paymentTerms = null,
        DateOnly? dueDate = null,
        AppEnvironmentMode environmentMode = AppEnvironmentMode.Production)
    {
        if (irn is null) throw new BadRequestException("IRN cannot be null", nameof(irn));
        if (invoiceType is null) throw new BadRequestException("Invoice type cannot be null", nameof(invoiceType));
        if (currency is null) throw new BadRequestException("Currency cannot be null", nameof(currency));
        if (deliveryPeriod is null) throw new BadRequestException("Delivery Period cannot be null", nameof(deliveryPeriod));
        if (issueDate > DateOnly.FromDateTime(DateTime.Now)) throw new BadRequestException("Issue Date Cannot Be Greater Than Today's Date");

        var invoice = new Invoice
        {
            InvoiceCode = new InvoiceId(invoicePrefix).FullId,
            BusinessId = businessId,
            PartyId = partyId,
            Irn = irn,
            IssueDate = issueDate,
            IssueTime = TimeOnly.FromDateTime(DateTime.Now),
            InvoiceType = invoiceType,
            InvoiceKind = invoiceKind,
            Currency = currency,
            DeliveryPeriod = deliveryPeriod,
            PaymentMeans = paymentMeans,
            PaymentTerms = paymentTerms,
            PaymentReference = paymentReference,
            PaymentStatus = PaymentStatus.Pending,
            DueDate = dueDate,
            InvoiceStatus = InvoiceStatus.APPROVED,
            Note = note,
            InvoiceSource = invoiceSource,
            EnvironmentMode = environmentMode
        };
        invoice.AddDomainEvent(new InvoiceCreatedEvent(invoice.Id, invoice.Irn, businessId));
        return invoice;
    }

    // Factory method for importing existing invoices from FIRS (skips future date validation)
    public static Invoice CreateFromImport(
        Guid businessId,
        Guid partyId,
        IRN irn,
        string invoicePrefix,
        DateOnly issueDate,
        TimeOnly? issueTime,
        InvoiceType invoiceType,
        Currency currency,
        DeliveryPeriod deliveryPeriod,
        PaymentMeans paymentMeans,
        InvoiceSource invoiceSource,
        InvoiceKind? invoiceKind = null,
        string? note = null,
        string? paymentReference = null,
        string? paymentTerms = null,
        DateOnly? dueDate = null,
        AppEnvironmentMode environmentMode = AppEnvironmentMode.Production)
    {
        if (irn is null) throw new BadRequestException("IRN cannot be null", nameof(irn));
        if (invoiceType is null) throw new BadRequestException("Invoice type cannot be null", nameof(invoiceType));
        if (currency is null) throw new BadRequestException("Currency cannot be null", nameof(currency));
        if (deliveryPeriod is null) throw new BadRequestException("Delivery Period cannot be null", nameof(deliveryPeriod));

        return new Invoice
        {
            InvoiceCode = new InvoiceId(invoicePrefix).FullId,
            BusinessId = businessId,
            PartyId = partyId,
            Irn = irn,
            IssueDate = issueDate,
            IssueTime = issueTime ?? TimeOnly.FromDateTime(DateTime.Now),
            InvoiceType = invoiceType,
            InvoiceKind = invoiceKind,
            Currency = currency,
            DeliveryPeriod = deliveryPeriod,
            PaymentMeans = paymentMeans,
            PaymentTerms = paymentTerms,
            PaymentReference = paymentReference,
            PaymentStatus = PaymentStatus.Pending,
            DueDate = dueDate,
            InvoiceStatus = InvoiceStatus.CREATED,
            Note = note,
            InvoiceSource = invoiceSource,
            EnvironmentMode = environmentMode
        };
    }

    // Factory method for draft invoices (for later completion)
    public static Invoice CreateDraft(
        Guid businessId,
        string invoicePrefix,
        InvoiceType invoiceType,
        Currency currency,
        AppEnvironmentMode environmentMode = AppEnvironmentMode.Production)
    {
        var draftInvoice = new Invoice
        {
            InvoiceCode = new InvoiceId($"{invoicePrefix}-DRAFT").FullId,
            BusinessId = businessId,
            IssueDate = DateOnly.FromDateTime(DateTime.Now),
            IssueTime = TimeOnly.FromDateTime(DateTime.Now),
            InvoiceType = invoiceType,
            Currency = currency,
            PaymentStatus = PaymentStatus.Pending,
            InvoiceStatus = InvoiceStatus.DRAFT,
            Note = "Draft Invoice - Incomplete",
            EnvironmentMode = environmentMode
        };

        return draftInvoice;
    }
    #endregion

    #region Business Logic Methods

    public void AddInvoiceItem(InvoiceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Seun & Kehny discussed with Justice GB Food to allow duplicate business items in an invoice
        //if (_invoiceLine.Any(i => i.BusinessItemId == item.BusinessItemId))
        //    throw new BadRequestException("Invoice item with this business item already exists");

        _invoiceLine.Add(item);
    }

    public void RemoveInvoiceItem(Guid businessItemId)
    {
        var item = _invoiceLine.FirstOrDefault(i => i.BusinessItemId == businessItemId);
        if (item is not null)
        {
            _invoiceLine.Remove(item);
        }
    }

    public void UpdatePaymentStatus(PaymentStatus newStatus, string? reference = null)
    {
        PaymentStatus = newStatus;
        if (newStatus == PaymentStatus.Paid && !string.IsNullOrWhiteSpace(reference))
            PaymentReference = reference;
    }

    /// <summary>
    /// Assigns this invoice to a VAT schedule, preventing it from being
    /// included in any subsequent schedule generation.
    /// </summary>
    public void AssignToVatSchedule(Guid scheduleId)
    {
        if (VatScheduleId.HasValue)
            throw new InvalidOperationException(
                $"Invoice {InvoiceCode} is already assigned to VAT schedule {VatScheduleId}.");
        VatScheduleId = scheduleId;
    }

    public void SubmitToFIRS(string submissionId)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
            throw new ArgumentException("Submission ID cannot be empty", nameof(submissionId));

        FIRSSubmissionId = submissionId;
        SubmittedToFIRSAt = DateTimeOffset.UtcNow;
        InvoiceStatus = InvoiceStatus.SUBMITTED;
    }

    public void SetQRCode(QRCode qrCode)
    {
        QRCode = qrCode ?? throw new ArgumentNullException(nameof(qrCode));
    }

    public void UpdatePaymentMeans(PaymentMeans newPaymentMeans)
    {
        PaymentMeans = newPaymentMeans;
    }

    public void UpdateNote(string note)
    {
        Note = note;
    }

    public void UpdatePaymentTerms(string paymentTerms)
    {
        PaymentTerms = paymentTerms;
    }

    public void UpdateStatus(InvoiceStatus newStatus)
    {
        InvoiceStatus = newStatus;
    }

    /// <summary>Sets the PartyId on an invoice that was created without one (e.g. vendor portal drafts).</summary>
    public void SetParty(Guid partyId)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId cannot be empty", nameof(partyId));
        PartyId = partyId;
    }

    public void SetQRCode(string encryptedData, byte[]? base64Image)
    {
        if (string.IsNullOrWhiteSpace(encryptedData))
            throw new ArgumentException("Encrypted data cannot be empty", nameof(encryptedData));

        QRCode = QRCode.Create(encryptedData, base64Image);
    }

    public void SetFIRSSubmissionResponseMessage(string? message)
    {
        FIRSSubmissionResponseMessage = message;
    }

    public void SetFIRSSubmissionId(string submissionId)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
            throw new ArgumentException("Submission ID cannot be empty", nameof(submissionId));

        FIRSSubmissionId = submissionId;
    }

    public void SetSubmittedToFIRSAt(DateTimeOffset submittedAt)
    {
        SubmittedToFIRSAt = submittedAt;
    }

    public void AddBillingReference(InvoiceBillingReference billingReference)
    {
        if (billingReference is null)
            throw new ArgumentNullException(nameof(billingReference));

        if (_billingReferences.Any(br => br.Irn.Value == billingReference.Irn.Value && br.IssueDate == billingReference.IssueDate))
            throw new BadRequestException("A billing reference with this IRN and issue date already exists");

        _billingReferences.Add(billingReference);
    }

    public void RemoveBillingReference(Guid billingReferenceId)
    {
        var reference = _billingReferences.FirstOrDefault(br => br.Id == billingReferenceId);
        if (reference is not null)
        {
            _billingReferences.Remove(reference);
        }
    }

    public void ClearBillingReferences()
    {
        _billingReferences.Clear();
    }

    // Dispatch Document Reference Methods
    public void SetDispatchDocumentReference(InvoiceDispatchDocumentReference dispatchReference)
    {
        if (dispatchReference is null)
            throw new ArgumentNullException(nameof(dispatchReference));

        DispatchDocumentReference = dispatchReference;
    }

    public void RemoveDispatchDocumentReference()
    {
        DispatchDocumentReference = null;
    }

    // Receipt Document Reference Methods
    public void SetReceiptDocumentReference(InvoiceReceiptDocumentReference receiptReference)
    {
        if (receiptReference is null)
            throw new ArgumentNullException(nameof(receiptReference));

        ReceiptDocumentReference = receiptReference;
    }

    public void RemoveReceiptDocumentReference()
    {
        ReceiptDocumentReference = null;
    }

    // Originator Document Reference Methods
    public void SetOriginatorDocumentReference(InvoiceOriginatorDocumentReference originatorReference)
    {
        if (originatorReference is null)
            throw new ArgumentNullException(nameof(originatorReference));

        OriginatorDocumentReference = originatorReference;
    }

    public void RemoveOriginatorDocumentReference()
    {
        OriginatorDocumentReference = null;
    }

    // Contract Document Reference Methods
    public void SetContractDocumentReference(InvoiceContractDocumentReference contractReference)
    {
        if (contractReference is null)
            throw new ArgumentNullException(nameof(contractReference));

        ContractDocumentReference = contractReference;
    }

    public void RemoveContractDocumentReference()
    {
        ContractDocumentReference = null;
    }

    // Additional Document References Methods (Collection)
    public void AddAdditionalDocumentReference(InvoiceAdditionalDocumentReference additionalReference)
    {
        if (additionalReference is null)
            throw new ArgumentNullException(nameof(additionalReference));

        if (_additionalDocumentReferences.Any(ar => ar.Irn.Value == additionalReference.Irn.Value && ar.IssueDate == additionalReference.IssueDate))
            throw new BadRequestException("An additional document reference with this IRN and issue date already exists");

        _additionalDocumentReferences.Add(additionalReference);
    }

    public void RemoveAdditionalDocumentReference(Guid additionalReferenceId)
    {
        var reference = _additionalDocumentReferences.FirstOrDefault(ar => ar.Id == additionalReferenceId);
        if (reference is not null)
        {
            _additionalDocumentReferences.Remove(reference);
        }
    }

    public void ClearAdditionalDocumentReferences()
    {
        _additionalDocumentReferences.Clear();
    }

    #endregion
}