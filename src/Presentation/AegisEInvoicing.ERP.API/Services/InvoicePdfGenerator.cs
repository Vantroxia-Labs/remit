using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.ERP.API.Services;

public static class InvoicePdfGenerator
{
    public static byte[] GenerateInvoicePdf(string jsonData, string? qrCode)
    {
        try
        {
            var invoiceData = JsonSerializer.Deserialize<InvoiceDocument>(jsonData, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });

            if (invoiceData is null)
                return GenerateFallbackPdf(jsonData);

            // Assign the qrCode directly if provided and non-empty
            byte[]? assignQrCode = null;

            // Convert base64 QR string to byte array safely
            if (!string.IsNullOrWhiteSpace(qrCode))
            {
                try
                {
                    assignQrCode = Convert.FromBase64String(
                        qrCode.Contains(',')
                            ? qrCode.Split(',')[1] // handles data:image/png;base64,<data>
                            : qrCode
                    );
                }
                catch (FormatException)
                {
                    assignQrCode = null; // Invalid base64; skip QR rendering
                }
            }

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                    page.Header().Element(c => ComposeHeader(c, invoiceData));
                    page.Content().Element(c => ComposeContent(c, invoiceData));
                    page.Footer().Element(c => ComposeFooter(c, assignQrCode));
                });
            }).GeneratePdf();
        }
        catch
        {
            return GenerateFallbackPdf(jsonData);
        }
    }

    // -------------------- HEADER --------------------
    private static void ComposeHeader(IContainer container, InvoiceDocument invoice)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            // Title
            column.Item().BorderBottom(2).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("INVOICE").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"IRN: {invoice.Irn ?? "N/A"}").FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Date: {invoice.IssueDate: dd MMM yyyy}").FontSize(10);
                    if (!string.IsNullOrEmpty(invoice.DueDate))
                        col.Item().Text($"Due Date: {invoice.DueDate:dd MMM yyyy}").FontSize(10);
                    col.Item().Text($"Status: {invoice.PaymentStatus ?? "N/A"}").FontSize(10).Bold();
                });
            });

            // Supplier and Customer
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("SUPPLIER").FontSize(12).Bold().FontColor(Colors.Blue.Darken1);
                    col.Item().PaddingTop(5).Column(supplierCol =>
                    {
                        var supplier = invoice.AccountingSupplierParty;
                        if (supplier != null)
                        {
                            supplierCol.Item().Text(supplier.PartyName ?? "N/A").Bold();
                            supplierCol.Item().Text($"TIN: {supplier.Tin ?? "N/A"}");
                            supplierCol.Item().Text($"Email: {supplier.Email ?? "N/A"}");
                            if (!string.IsNullOrEmpty(supplier.Telephone))
                                supplierCol.Item().Text($"Tel: {supplier.Telephone}");

                            if (supplier.PostalAddress != null)
                            {
                                var address = supplier.PostalAddress;
                                supplierCol.Item().Text($"{address.StreetName ?? ""}");
                                supplierCol.Item().Text($"{address.CityName ?? ""}, {address.PostalZone ?? ""}");
                                supplierCol.Item().Text($"{address.Country ?? ""}");
                            }
                        }
                    });
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("CUSTOMER").FontSize(12).Bold().FontColor(Colors.Blue.Darken1);
                    col.Item().PaddingTop(5).Column(customerCol =>
                    {
                        var customer = invoice.AccountingCustomerParty;
                        if (customer != null)
                        {
                            customerCol.Item().Text(customer.PartyName ?? "N/A").Bold();
                            customerCol.Item().Text($"TIN: {customer.Tin ?? "N/A"}");
                            customerCol.Item().Text($"Email: {customer.Email ?? "N/A"}");
                            if (!string.IsNullOrEmpty(customer.Telephone))
                                customerCol.Item().Text($"Tel: {customer.Telephone}");

                            if (customer.PostalAddress != null)
                            {
                                var address = customer.PostalAddress;
                                customerCol.Item().Text($"{address.StreetName ?? ""}");
                                customerCol.Item().Text($"{address.CityName ?? ""}, {address.PostalZone ?? ""}");
                                customerCol.Item().Text($"{address.Country ?? ""}");
                            }
                        }
                    });
                });
            });
        });
    }

    // -------------------- CONTENT --------------------
    private static void ComposeContent(IContainer container, InvoiceDocument invoice)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Spacing(10);

            if (!string.IsNullOrEmpty(invoice.Note))
                column.Item().Background(Colors.Grey.Lighten3).Padding(10).Text($"Note: {invoice.Note}").FontSize(9);

            // Invoice table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                });

                // Header
                table.Header(header =>
                {
                    static IContainer Cell(IContainer c) =>
                        c.Background(Colors.Blue.Darken2).Padding(5).BorderBottom(1).BorderColor(Colors.White);
                    header.Cell().Element(Cell).Text("#").Bold().FontColor(Colors.White);
                    header.Cell().Element(Cell).Text("Item Description").Bold().FontColor(Colors.White);
                    header.Cell().Element(Cell).AlignRight().Text("Qty").Bold().FontColor(Colors.White);
                    header.Cell().Element(Cell).AlignRight().Text("Unit Price").Bold().FontColor(Colors.White);
                    header.Cell().Element(Cell).AlignRight().Text("Discount").Bold().FontColor(Colors.White);
                    header.Cell().Element(Cell).AlignRight().Text("Amount").Bold().FontColor(Colors.White);
                });

                // Lines
                if (invoice.InvoiceLine?.Any() == true)
                {
                    int n = 1;
                    foreach (var line in invoice.InvoiceLine)
                    {
                        var bg = n % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                        static IContainer Style(IContainer c, string color) =>
                            c.Background(color).Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                        table.Cell().Element(c => Style(c, bg)).Text(n.ToString());
                        table.Cell().Element(c => Style(c, bg)).Column(col =>
                        {
                            col.Item().Text(line.Item?.Name ?? "N/A").Bold();
                            if (!string.IsNullOrEmpty(line.Item?.Description))
                                col.Item().Text(line.Item.Description).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        table.Cell().Element(c => Style(c, bg)).AlignRight().Text(line.InvoicedQuantity.ToString());
                        table.Cell().Element(c => Style(c, bg)).AlignRight().Text($"{line.Price?.PriceAmount:N2}");
                        table.Cell().Element(c => Style(c, bg)).AlignRight().Text($"{line.DiscountAmount:N2}");
                        table.Cell().Element(c => Style(c, bg)).AlignRight().Text($"{line.LineExtensionAmount:N2}");
                        n++;
                    }
                }
            });

            // Totals
            column.Item().AlignRight().PaddingTop(10).Column(total =>
            {
                var monetary = invoice.LegalMonetaryTotal;
                if (monetary != null)
                {
                    total.Item().Row(row =>
                    {
                        row.ConstantItem(150).Text("Subtotal:");
                        row.ConstantItem(100).AlignRight().Text($"{monetary.LineExtensionAmount:N2}");
                    });

                    if (invoice.TaxTotal?.Any() == true)
                    {
                        var totalTax = invoice.TaxTotal.Sum(t => t.TaxAmount);
                        total.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text("Tax:");
                            row.ConstantItem(100).AlignRight().Text($"{totalTax:N2}");
                        });
                    }

                    total.Item().PaddingTop(5).BorderTop(2).BorderColor(Colors.Blue.Darken2).Row(row =>
                    {
                        row.ConstantItem(150).Text("TOTAL:").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                        row.ConstantItem(100).AlignRight()
                            .Text($"{invoice.DocumentCurrencyCode ?? ""} {monetary.PayableAmount:N2}")
                            .Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                    });
                }
            });

            if (!string.IsNullOrEmpty(invoice.PaymentTermsNote))
            {
                column.Item().PaddingTop(15).Column(col =>
                {
                    col.Item().Text("Payment Terms").FontSize(11).Bold();
                    col.Item().PaddingTop(5).Text(invoice.PaymentTermsNote).FontSize(9);
                });
            }
        });
    }

    // -------------------- FOOTER --------------------
    private static void ComposeFooter(IContainer container, byte[]? qrBytes)
    {
        container.AlignBottom().PaddingVertical(10).Column(column =>
        {
            column.Spacing(6);

            // QR code section (bottom-centered)
            if (qrBytes is { Length: > 0 })
            {
                column.Item().AlignCenter().Column(inner =>
                {
                    inner.Item().AlignCenter()
                        .Width(160)
                        .Height(160)
                        .Image(qrBytes)
                        .FitArea()
                        .WithCompressionQuality(ImageCompressionQuality.High);

                    inner.Item().AlignCenter()
                        .Text("Scan QR Code to verify invoice")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);

                    inner.Item().AlignCenter()
                        .Text("Digitally authenticated document")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2)
                        .Italic();
                });
            }

            // Divider and page metadata
            column.Item()
                .PaddingTop(8)
                .BorderTop(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Row(row =>
                {
                    row.RelativeItem().AlignLeft().Text(t =>
                    {
                        t.Span("Generated: ").FontSize(8);
                        t.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"))
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    row.RelativeItem().AlignCenter().Text(t =>
                    {
                        t.Span("Page ").FontSize(8);
                        t.CurrentPageNumber().FontSize(8);
                        t.Span(" of ").FontSize(8);
                        t.TotalPages().FontSize(8);
                    });

                    row.RelativeItem().AlignRight()
                        .Text("System-generated document")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });
        });
    }

    // -------------------- FALLBACK --------------------
    private static byte[] GenerateFallbackPdf(string raw)
    {
        return Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(40);
                p.Header().Text("Invoice Document").FontSize(20).Bold();
                p.Content().Padding(10).Text(raw).FontSize(9).FontFamily("Courier New");
                p.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page "); t.CurrentPageNumber(); t.Span(" of "); t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    // Model classes for JSON deserialization
    public class AccountingCustomerParty
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("postal_address_id")]
        public string? PostalAddressId { get; set; }

        [JsonPropertyName("party_name")]
        public string? PartyName { get; set; }

        [JsonPropertyName("tin")]
        public string? Tin { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("telephone")]
        public string? Telephone { get; set; }

        [JsonPropertyName("business_description")]
        public string? BusinessDescription { get; set; }

        [JsonPropertyName("postal_address")]
        public PostalAddress? PostalAddress { get; set; }
    }

    public class AccountingSupplierParty
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("postal_address_id")]
        public string? PostalAddressId { get; set; }

        [JsonPropertyName("party_name")]
        public string? PartyName { get; set; }

        [JsonPropertyName("tin")]
        public string? Tin { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("telephone")]
        public string? Telephone { get; set; }

        [JsonPropertyName("business_description")]
        public string? BusinessDescription { get; set; }

        [JsonPropertyName("postal_address")]
        public PostalAddress? PostalAddress { get; set; }
    }

    public class AllowanceCharge
    {
        [JsonPropertyName("charge_indicator")]
        public bool ChargeIndicator { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }
    }

    public class InvoiceDeliveryPeriod
    {
        [JsonPropertyName("start_date")]
        public string? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }
    }

    public class InvoiceLine
    {
        [JsonPropertyName("invoiced_quantity")]
        public decimal InvoicedQuantity { get; set; }

        [JsonPropertyName("line_extension_amount")]
        public decimal LineExtensionAmount { get; set; }

        [JsonPropertyName("item")]
        public Item? Item { get; set; }

        [JsonPropertyName("price")]
        public Price? Price { get; set; }

        [JsonPropertyName("hsn_code")]
        public string? HsnCode { get; set; }

        [JsonPropertyName("product_category")]
        public string? ProductCategory { get; set; }

        [JsonPropertyName("discount_rate")]
        public decimal DiscountRate { get; set; }

        [JsonPropertyName("discount_amount")]
        public decimal DiscountAmount { get; set; }

        [JsonPropertyName("fee_rate")]
        public decimal FeeRate { get; set; }

        [JsonPropertyName("fee_amount")]
        public decimal FeeAmount { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("sellers_item_identification")]
        public object? SellersItemIdentification { get; set; }
    }

    public class LegalMonetaryTotal
    {
        [JsonPropertyName("line_extension_amount")]
        public decimal LineExtensionAmount { get; set; }

        [JsonPropertyName("tax_exclusive_amount")]
        public decimal TaxExclusiveAmount { get; set; }

        [JsonPropertyName("tax_inclusive_amount")]
        public decimal TaxInclusiveAmount { get; set; }

        [JsonPropertyName("payable_amount")]
        public decimal PayableAmount { get; set; }
    }

    public class PaymentMean
    {
        [JsonPropertyName("payment_means_code")]
        public string? PaymentMeansCode { get; set; }

        [JsonPropertyName("payment_due_date")]
        public string? PaymentDueDate { get; set; }
    }

    public class PostalAddress
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("street_name")]
        public string? StreetName { get; set; }

        [JsonPropertyName("city_name")]
        public string? CityName { get; set; }

        [JsonPropertyName("postal_zone")]
        public string? PostalZone { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }

    public class Price
    {
        [JsonPropertyName("price_amount")]
        public decimal PriceAmount { get; set; }

        [JsonPropertyName("base_quantity")]
        public int BaseQuantity { get; set; }

        [JsonPropertyName("price_unit")]
        public string? PriceUnit { get; set; }
    }

    public class InvoiceDocument
    {
        [JsonPropertyName("irn")]
        public string? Irn { get; set; }

        [JsonPropertyName("payment_status")]
        public string? PaymentStatus { get; set; }

        [JsonPropertyName("customization_id")]
        public object? CustomizationId { get; set; }

        [JsonPropertyName("issue_date")]
        public string? IssueDate { get; set; }

        [JsonPropertyName("issue_time")]
        public string? IssueTime { get; set; }

        [JsonPropertyName("due_date")]
        public string? DueDate { get; set; }

        [JsonPropertyName("invoice_type_code")]
        public string? InvoiceTypeCode { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("document_currency_code")]
        public string? DocumentCurrencyCode { get; set; }

        [JsonPropertyName("tax_currency_code")]
        public string? TaxCurrencyCode { get; set; }

        [JsonPropertyName("invoice_delivery_period")]
        public InvoiceDeliveryPeriod? InvoiceDeliveryPeriod { get; set; }

        [JsonPropertyName("accounting_supplier_party")]
        public AccountingSupplierParty? AccountingSupplierParty { get; set; }

        [JsonPropertyName("accounting_customer_party")]
        public AccountingCustomerParty? AccountingCustomerParty { get; set; }

        [JsonPropertyName("payment_means")]
        public List<PaymentMean> PaymentMeans { get; set; } = [];

        [JsonPropertyName("payment_terms_note")]
        public string? PaymentTermsNote { get; set; }

        [JsonPropertyName("allowance_charge")]
        public List<AllowanceCharge> AllowanceCharge { get; set; } = [];

        [JsonPropertyName("tax_total")]
        public List<TaxTotal> TaxTotal { get; set; } = [];

        [JsonPropertyName("legal_monetary_total")]
        public LegalMonetaryTotal? LegalMonetaryTotal { get; set; }

        [JsonPropertyName("invoice_line")]
        public List<InvoiceLine> InvoiceLine { get; set; } = [];
    }

    public class TaxCategory
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("percent")]
        public decimal? Percent { get; set; }
    }

    public class TaxSubtotal
    {
        [JsonPropertyName("taxable_amount")]
        public decimal? TaxableAmount { get; set; }

        [JsonPropertyName("tax_amount")]
        public decimal? TaxAmount { get; set; }

        [JsonPropertyName("tax_category")]
        public TaxCategory? TaxCategory { get; set; }
    }

    public class TaxTotal
    {
        [JsonPropertyName("tax_amount")]
        public decimal? TaxAmount { get; set; }

        [JsonPropertyName("tax_subtotal")]
        public List<TaxSubtotal> TaxSubtotal { get; set; } = [];
    }

}