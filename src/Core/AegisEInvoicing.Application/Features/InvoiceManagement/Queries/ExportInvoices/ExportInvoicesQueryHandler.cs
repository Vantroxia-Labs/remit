using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.ExportInvoices;

public class ExportInvoicesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<ExportInvoicesQueryHandler> logger)
    : IRequestHandler<ExportInvoicesQuery, ExportInvoicesResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<ExportInvoicesQueryHandler> _logger = logger;

    public async Task<ExportInvoicesResult> Handle(ExportInvoicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Authorization check
            if (!IsUserAuthorized())
                return (ExportInvoicesResult)ExportInvoicesResult.AuthorizationError();

            // Build query with filters
            var query = _context.Invoices
                .Include(i => i.Party)
                    .ThenInclude(p => p.Address)
                .Include(i => i.InvoiceLine)
                    .ThenInclude(il => il.BusinessItem)
                        .ThenInclude(bi => bi!.ItemCategory)
                .Where(i => i.BusinessId == _currentUser.BusinessId);

            // Apply filters
            query = query.Where(i => i.BusinessId == _currentUser.BusinessId!.Value);

            if (request.InvoiceStatus.HasValue)
                query = query.Where(i => i.InvoiceStatus == request.InvoiceStatus.Value);

            if (request.PaymentStatus.HasValue)
                query = query.Where(i => i.PaymentStatus == request.PaymentStatus.Value);

            if (request.StartDate.HasValue)
                query = query.Where(i => i.IssueDate >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(i => i.IssueDate <= request.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(i =>
                    i.InvoiceCode.ToLower().Contains(searchTerm) ||
                    i.Irn.Value.ToLower().Contains(searchTerm) ||
                    (i.PaymentReference != null && i.PaymentReference.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentReference))
                query = query.Where(i => i.PaymentReference == request.PaymentReference);

            // Order by PaymentReference for grouping
            var invoices = await query
                .OrderBy(i => i.PaymentReference)
                .ThenBy(i => i.IssueDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (!invoices.Any())
            {
                return (ExportInvoicesResult)ExportInvoicesResult.NotFound("No invoices found to export");
            }

            // Generate Excel file
            var fileContents = GenerateExcelFile(invoices);
            var fileName = $"Invoices_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            var totalItems = invoices.Sum(i => i.InvoiceLine.Count);

            _logger.LogInformation(
                "Exported {InvoiceCount} invoices with {ItemCount} items for business {BusinessId}",
                invoices.Count, totalItems, _currentUser.BusinessId);

            return new ExportInvoicesResult
            {
                IsSuccess = true,
                FileContents = fileContents,
                FileName = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                TotalInvoices = invoices.Count,
                TotalItems = totalItems,
                Message = $"Successfully exported {invoices.Count} invoices with {totalItems} items",
                StatusCodes = Domain.Enums.HttpStatusCodes.OK.ToInt()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoices for business {BusinessId}", _currentUser.BusinessId);
            return (ExportInvoicesResult)ExportInvoicesResult.InternalServerError(
                "An error occurred while exporting invoices. Please try again later.");
        }
    }

    private byte[] GenerateExcelFile(List<Domain.Entities.InvoiceManagement.Invoice> invoices)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Invoices");

        // Define headers (3 rows as per upload format)
        CreateHeaders(worksheet);

        // Populate data - each invoice item gets its own row, grouped by PaymentReference
        int row = 4; // Data starts at row 4

        foreach (var invoice in invoices)
        {
            var party = invoice.Party;
            var invoiceItems = invoice.InvoiceLine.ToList();

            // If no items, create one row with invoice data only
            if (!invoiceItems.Any())
            {
                PopulateInvoiceRow(worksheet, row, invoice, party, null);
                row++;
                continue;
            }

            // Each invoice item gets its own row
            foreach (var item in invoiceItems)
            {
                PopulateInvoiceRow(worksheet, row, invoice, party, item);
                row++;
            }
        }

        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // Set minimum column widths
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            if (worksheet.Column(col).Width < 10)
                worksheet.Column(col).Width = 10;
            if (worksheet.Column(col).Width > 50)
                worksheet.Column(col).Width = 50;
        }

        // Apply borders to all cells with data
        var dataRange = worksheet.Cells[1, 1, row - 1, worksheet.Dimension.End.Column];
        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

        return package.GetAsByteArray();
    }

    private void CreateHeaders(ExcelWorksheet worksheet)
    {
        // Row 1: Main categories
        worksheet.Cells[1, 1].Value = "IssueDate";
        worksheet.Cells[1, 2].Value = "DueDate";
        worksheet.Cells[1, 3].Value = "IssueTime";
        worksheet.Cells[1, 4].Value = "InvoiceType";
        worksheet.Cells[1, 5].Value = "InvoiceType";
        worksheet.Cells[1, 6].Value = "Currency";
        worksheet.Cells[1, 7].Value = "Currency";
        worksheet.Cells[1, 8].Value = "DeliveryPeriod";
        worksheet.Cells[1, 9].Value = "DeliveryPeriod";
        worksheet.Cells[1, 10].Value = "PaymentMeans";
        worksheet.Cells[1, 11].Value = "PaymentMeans";
        worksheet.Cells[1, 12].Value = "Note";
        worksheet.Cells[1, 13].Value = "PaymentReference";
        worksheet.Cells[1, 14].Value = "PaymentTerms";
        worksheet.Cells[1, 15].Value = "Party";
        worksheet.Cells[1, 16].Value = "Party";
        worksheet.Cells[1, 17].Value = "Party";
        worksheet.Cells[1, 18].Value = "Party";
        worksheet.Cells[1, 19].Value = "Party";
        worksheet.Cells[1, 20].Value = "Party";
        worksheet.Cells[1, 21].Value = "Party";
        worksheet.Cells[1, 22].Value = "Party";
        worksheet.Cells[1, 23].Value = "Party";
        worksheet.Cells[1, 24].Value = "Party";
        worksheet.Cells[1, 25].Value = "Party";
        worksheet.Cells[1, 26].Value = "InvoiceItems";
        worksheet.Cells[1, 27].Value = "InvoiceItems";
        worksheet.Cells[1, 28].Value = "InvoiceItems";
        worksheet.Cells[1, 29].Value = "InvoiceItems";
        worksheet.Cells[1, 30].Value = "InvoiceItems";
        worksheet.Cells[1, 31].Value = "InvoiceItems";
        worksheet.Cells[1, 32].Value = "InvoiceItems";
        worksheet.Cells[1, 33].Value = "InvoiceItems";
        worksheet.Cells[1, 34].Value = "InvoiceItems";
        worksheet.Cells[1, 35].Value = "InvoiceItems";
        worksheet.Cells[1, 36].Value = "InvoiceItems";
        worksheet.Cells[1, 37].Value = "InvoiceItems";
        worksheet.Cells[1, 38].Value = "InvoiceItems";

        // Row 2: Sub-categories
        worksheet.Cells[2, 1].Value = "";
        worksheet.Cells[2, 2].Value = "";
        worksheet.Cells[2, 3].Value = "";
        worksheet.Cells[2, 4].Value = "";
        worksheet.Cells[2, 5].Value = "";
        worksheet.Cells[2, 6].Value = "";
        worksheet.Cells[2, 7].Value = "";
        worksheet.Cells[2, 8].Value = "";
        worksheet.Cells[2, 9].Value = "";
        worksheet.Cells[2, 10].Value = "";
        worksheet.Cells[2, 11].Value = "";
        worksheet.Cells[2, 12].Value = "";
        worksheet.Cells[2, 13].Value = "";
        worksheet.Cells[2, 14].Value = "";
        worksheet.Cells[2, 15].Value = "";
        worksheet.Cells[2, 16].Value = "";
        worksheet.Cells[2, 17].Value = "";
        worksheet.Cells[2, 18].Value = "";
        worksheet.Cells[2, 19].Value = "";
        worksheet.Cells[2, 20].Value = "Address";
        worksheet.Cells[2, 21].Value = "Address";
        worksheet.Cells[2, 22].Value = "Address";
        worksheet.Cells[2, 23].Value = "Address";
        worksheet.Cells[2, 24].Value = "Address";
        worksheet.Cells[2, 25].Value = "";
        worksheet.Cells[2, 26].Value = "";
        worksheet.Cells[2, 27].Value = "";
        worksheet.Cells[2, 28].Value = "";
        worksheet.Cells[2, 29].Value = "ServiceCode";
        worksheet.Cells[2, 30].Value = "ServiceCode";
        worksheet.Cells[2, 31].Value = "TaxCategory";
        worksheet.Cells[2, 32].Value = "TaxCategory";
        worksheet.Cells[2, 33].Value = "";
        worksheet.Cells[2, 34].Value = "";
        worksheet.Cells[2, 35].Value = "DiscountFee";
        worksheet.Cells[2, 36].Value = "DiscountFee";
        worksheet.Cells[2, 37].Value = "AdditionalFee";
        worksheet.Cells[2, 38].Value = "AdditionalFee";

        // Row 3: Field names
        worksheet.Cells[3, 1].Value = "IssueDate";
        worksheet.Cells[3, 2].Value = "DueDate";
        worksheet.Cells[3, 3].Value = "IssueTime";
        worksheet.Cells[3, 4].Value = "Name";
        worksheet.Cells[3, 5].Value = "Code";
        worksheet.Cells[3, 6].Value = "Name";
        worksheet.Cells[3, 7].Value = "Code";
        worksheet.Cells[3, 8].Value = "StartDate";
        worksheet.Cells[3, 9].Value = "EndDate";
        worksheet.Cells[3, 10].Value = "Code";
        worksheet.Cells[3, 11].Value = "Name";
        worksheet.Cells[3, 12].Value = "Note";
        worksheet.Cells[3, 13].Value = "PaymentReference";
        worksheet.Cells[3, 14].Value = "PaymentTerms";
        worksheet.Cells[3, 15].Value = "Name";
        worksheet.Cells[3, 16].Value = "Description";
        worksheet.Cells[3, 17].Value = "Phone";
        worksheet.Cells[3, 18].Value = "Email";
        worksheet.Cells[3, 19].Value = "TaxIdentificationNumber";
        worksheet.Cells[3, 20].Value = "Street";
        worksheet.Cells[3, 21].Value = "City";
        worksheet.Cells[3, 22].Value = "State";
        worksheet.Cells[3, 23].Value = "Country";
        worksheet.Cells[3, 24].Value = "PostalCode";
        worksheet.Cells[3, 25].Value = "Description";
        worksheet.Cells[3, 26].Value = "Name";
        worksheet.Cells[3, 27].Value = "ItemDescription";
        worksheet.Cells[3, 28].Value = "ItemCategory";
        worksheet.Cells[3, 29].Value = "Code";
        worksheet.Cells[3, 30].Value = "Name";
        worksheet.Cells[3, 31].Value = "Name";
        worksheet.Cells[3, 32].Value = "Percent";
        worksheet.Cells[3, 33].Value = "UnitPrice";
        worksheet.Cells[3, 34].Value = "Quantity";
        worksheet.Cells[3, 35].Value = "Amount";
        worksheet.Cells[3, 36].Value = "FeeStandardUnit";
        worksheet.Cells[3, 37].Value = "Amount";
        worksheet.Cells[3, 38].Value = "FeeStandardUnit";

        // Style headers
        var headerRange = worksheet.Cells[1, 1, 3, 38];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        // Freeze header rows
        worksheet.View.FreezePanes(4, 1);
    }

    private void PopulateInvoiceRow(
        ExcelWorksheet worksheet,
        int row,
        Domain.Entities.InvoiceManagement.Invoice invoice,
        Domain.Entities.InvoiceManagement.Party party,
        Domain.Entities.InvoiceManagement.InvoiceItem? item)
    {
        // Invoice data
        worksheet.Cells[row, 1].Value = invoice.IssueDate.ToString("yyyy-MM-dd");
        worksheet.Cells[row, 2].Value = invoice.DueDate?.ToString("yyyy-MM-dd") ?? "";
        worksheet.Cells[row, 3].Value = invoice.IssueTime?.ToString("HH:mm:ss") ?? "";
        worksheet.Cells[row, 4].Value = invoice.InvoiceType.Name;
        worksheet.Cells[row, 5].Value = invoice.InvoiceType.Code;
        worksheet.Cells[row, 6].Value = invoice.Currency.Name;
        worksheet.Cells[row, 7].Value = invoice.Currency.Code;
        worksheet.Cells[row, 8].Value = invoice.DeliveryPeriod.StartDate.ToString("yyyy-MM-dd");
        worksheet.Cells[row, 9].Value = invoice.DeliveryPeriod.EndDate.ToString("yyyy-MM-dd");
        worksheet.Cells[row, 10].Value = invoice.PaymentMeans?.Code ?? "";
        worksheet.Cells[row, 11].Value = invoice.PaymentMeans?.Name ?? "";
        worksheet.Cells[row, 12].Value = invoice.Note ?? "";
        worksheet.Cells[row, 13].Value = invoice.PaymentReference ?? "";
        worksheet.Cells[row, 14].Value = invoice.PaymentTerms ?? "";

        // Party data
        worksheet.Cells[row, 15].Value = party.Name;
        worksheet.Cells[row, 16].Value = party.Description ?? "";
        worksheet.Cells[row, 17].Value = party.Phone;
        worksheet.Cells[row, 18].Value = party.Email;
        worksheet.Cells[row, 19].Value = party.TaxIdentificationNumber.Value;
        worksheet.Cells[row, 20].Value = party.Address.Street;
        worksheet.Cells[row, 21].Value = party.Address.City;
        worksheet.Cells[row, 22].Value = party.Address.State;
        worksheet.Cells[row, 23].Value = party.Address.Country;
        worksheet.Cells[row, 24].Value = party.Address.PostalCode;
        worksheet.Cells[row, 25].Value = party.Description ?? "";

        // Invoice item data (if exists)
        if (item != null)
        {
            var businessItem = item.BusinessItem;
            worksheet.Cells[row, 26].Value = businessItem!.Name;
            worksheet.Cells[row, 27].Value = businessItem!.ItemDescription;
            worksheet.Cells[row, 28].Value = businessItem!.ItemCategory?.Name ?? "";
            worksheet.Cells[row, 29].Value = businessItem!.ServiceCode!.Code;
            worksheet.Cells[row, 30].Value = businessItem!.ServiceCode!.Name;
            worksheet.Cells[row, 31].Value = businessItem!.ItemType.ToString();
            worksheet.Cells[row, 32].Value = "";
            worksheet.Cells[row, 33].Value = businessItem!.UnitPrice;
            worksheet.Cells[row, 34].Value = item.Quantity;
            worksheet.Cells[row, 35].Value = item.DiscountFee?.Amount ?? 0;
            worksheet.Cells[row, 36].Value = item.DiscountFee?.Code.ToString() ?? "";
            worksheet.Cells[row, 37].Value = item.AdditionalFee?.Amount ?? 0;
            worksheet.Cells[row, 38].Value = item.AdditionalFee?.Code.ToString() ?? "";
        }
    }

    private bool IsUserAuthorized() =>
        _currentUser?.UserId.HasValue == true && _currentUser?.BusinessId.HasValue == true;
}
