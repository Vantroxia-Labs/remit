namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ImportFirsInvoices;

public record MbsInvoiceListData(
    IReadOnlyList<MbsInvoiceListItem> Items,
    MbsPageInfo Page);

public record MbsInvoiceListItem(
    string Irn,
    string EntryStatus,
    string InvoiceTypeCode,
    string IssueDate,
    string IssueTime,
    string? DueDate,
    string DocumentCurrencyCode);

public record MbsPageInfo(
    int Page,
    int Size,
    bool HasNextPage,
    int TotalCount);

public record MbsInvoiceDetail(
    string Irn,
    string InvoiceTypeCode,
    string IssueDate,
    string IssueTime,
    string? DueDate,
    string? Note,
    string DocumentCurrencyCode,
    MbsDeliveryPeriod? DeliveryPeriod,
    MbsParty? SupplierParty,
    MbsParty? CustomerParty,
    IReadOnlyList<MbsPaymentMeans> PaymentMeans,
    string? PaymentTermsNote,
    IReadOnlyList<MbsTaxTotal> TaxTotal,
    IReadOnlyList<MbsInvoiceLine> InvoiceLine);

public record MbsDeliveryPeriod(string StartDate, string EndDate);

public record MbsParty(
    string PartyName,
    string Tin,
    string Email,
    string Telephone,
    string? BusinessDescription,
    MbsPostalAddress? PostalAddress);

public record MbsPostalAddress(
    string StreetName,
    string CityName,
    string? PostalZone,
    string? State,
    string Country);

public record MbsPaymentMeans(string PaymentMeansCode);

public record MbsTaxTotal(
    decimal TaxAmount,
    IReadOnlyList<MbsTaxSubtotal> TaxSubtotal);

public record MbsTaxSubtotal(
    decimal TaxableAmount,
    decimal TaxAmount,
    MbsTaxCategory? TaxCategory);

public record MbsTaxCategory(string Id, decimal Percent);

public record MbsInvoiceLine(
    decimal InvoicedQuantity,
    decimal LineExtensionAmount,
    MbsItem? Item,
    MbsPrice? Price,
    string HsnCode,
    string ProductCategory,
    string ServiceCategory,
    decimal DiscountRate,
    decimal DiscountAmount,
    decimal FeeRate,
    decimal FeeAmount);

public record MbsItem(string Name, string? Description);

public record MbsPrice(decimal PriceAmount, decimal BaseQuantity);
