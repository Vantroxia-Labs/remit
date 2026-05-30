using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.VatScheduleExport.Dto;
using AutoMapper;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.VatScheduleExport
{
    public class ExportVatScheduleQueryHandler : IRequestHandler<ExportVatScheduleQuery, byte[]>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ExportVatScheduleQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<byte[]> Handle(ExportVatScheduleQuery request, CancellationToken cancellationToken)
        {
            var vatSchedule = await _context.VatSchedules
                .Include(s => s.Items)
                .Include(s => s.InputItems)
                .Include(s => s.Business)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (vatSchedule == null)
                return Array.Empty<byte>();

            var business = vatSchedule.Business;
            var exportDtos = _mapper.Map<List<VatScheduleItemExportDto>>(vatSchedule.Items);

            var inputExportDtos = vatSchedule.InputItems
                .OrderBy(i => i.IssueDate)
                .Select(i => new InputVatScheduleItemExportDto
                {
                    Irn = i.Irn,
                    SupplierName = i.SupplierName,
                    SupplierTin = i.SupplierTin,
                    IssueDate = i.IssueDate,
                    TaxableAmount = i.TaxableAmount,
                    VatAmount = i.VatAmount,
                    TotalAmount = i.TotalAmount,
                })
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("VAT Schedule");

            var naira = "_(\"₦\"* #,##0.00_);_(\"₦\"* (#,##0.00);_(\"₦\"* \"-\"??_);_(@_)";

            // ── Business header (rows 1–6) ───────────────────────────────────
            ws.Cell("A1").Value = business.Name;
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Range("A1:I1").Merge();

            ws.Cell("A2").Value = $"TIN: {business.TaxIdentificationNumber}";
            ws.Cell("A2").Style.Font.FontSize = 11;
            ws.Range("A2:I2").Merge();

            var addr = business.RegisteredAddress;
            ws.Cell("A3").Value = $"{addr.Street}, {addr.City}, {addr.State}, {addr.Country}";
            ws.Cell("A3").Style.Font.FontSize = 10;
            ws.Cell("A3").Style.Font.FontColor = XLColor.Gray;
            ws.Range("A3:I3").Merge();

            ws.Cell("A4").Value = $"Output VAT Schedule — {vatSchedule.MonthName} {vatSchedule.Year}";
            ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("A4").Style.Font.FontSize = 12;
            ws.Range("A4:I4").Merge();

            ws.Cell("A5").Value = $"Period: {vatSchedule.PeriodStart:dd MMM yyyy} – {vatSchedule.PeriodEnd:dd MMM yyyy}";
            ws.Cell("E5").Value = $"Filing Due: {vatSchedule.DueDate:dd MMM yyyy}";
            ws.Cell("E5").Style.Font.Bold = true;
            ws.Cell("E5").Style.Font.FontColor =
                vatSchedule.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)
                    ? XLColor.Red
                    : XLColor.DarkGreen;

            ws.Cell("A6").Value = $"Status: {vatSchedule.Status}";
            if (vatSchedule.FiledAt.HasValue)
                ws.Cell("E6").Value = $"Filed: {vatSchedule.FiledAt.Value:dd MMM yyyy HH:mm} UTC";

            ws.Row(1).Height = 20;
            ws.Row(4).Height = 18;

            // ── Summary block (rows 8–15) ─────────────────────────────────────
            ws.Cell("A8").Value = "Output VAT";
            ws.Cell("A8").Style.Font.Bold = true;
            ws.Cell("A8").Style.Font.FontSize = 11;

            ws.Cell("A9").Value = "Total Output Invoices";
            ws.Cell("B9").Value = vatSchedule.TotalInvoiceCount;
            ws.Cell("A10").Value = "Total Taxable Amount (Output)";
            ws.Cell("B10").Value = vatSchedule.TotalTaxableAmount;
            ws.Cell("B10").Style.NumberFormat.Format = naira;
            ws.Cell("A11").Value = "Total Output VAT";
            ws.Cell("B11").Value = vatSchedule.TotalVatAmount;
            ws.Cell("B11").Style.NumberFormat.Format = naira;

            ws.Cell("A13").Value = "Input VAT";
            ws.Cell("A13").Style.Font.Bold = true;
            ws.Cell("A13").Style.Font.FontSize = 11;

            ws.Cell("A14").Value = "Total Input Invoices";
            ws.Cell("B14").Value = vatSchedule.TotalInputInvoiceCount;
            ws.Cell("A15").Value = "Total Taxable Amount (Input)";
            ws.Cell("B15").Value = vatSchedule.TotalInputTaxableAmount;
            ws.Cell("B15").Style.NumberFormat.Format = naira;
            ws.Cell("A16").Value = "Total Input VAT";
            ws.Cell("B16").Value = vatSchedule.TotalInputVatAmount;
            ws.Cell("B16").Style.NumberFormat.Format = naira;

            ws.Cell("A18").Value = "Net VAT Payable";
            ws.Cell("B18").Value = vatSchedule.NetVatPayable;
            ws.Cell("B18").Style.NumberFormat.Format = naira;
            ws.Cell("A18").Style.Font.Bold = true;
            ws.Cell("B18").Style.Font.Bold = true;
            ws.Cell("A18").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
            ws.Cell("B18").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");

            ws.Range("A9:A11").Style.Font.Bold = true;
            ws.Range("A14:A16").Style.Font.Bold = true;

            // ── Output VAT section label (row 20) ───────────────────────────
            ws.Cell("A20").Value = "Section A: Output VAT — Invoices Issued";
            ws.Cell("A20").Style.Font.Bold = true;
            ws.Cell("A20").Style.Font.FontSize = 11;
            ws.Range("A20:J20").Merge();

            // ── Output VAT column headers (row 21) ───────────────────────────
            var columns = new[]
            {
                "S/N", "Invoice Code", "IRN", "Party Name", "Party TIN",
                "Issue Date", "Taxable Amount (₦)", "VAT 7.5% (₦)", "Total Amount (₦)", "Payment Status"
            };

            for (int i = 0; i < columns.Length; i++)
                ws.Cell(21, i + 1).Value = columns[i];

            var headerRange = ws.Range(21, 1, 21, columns.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
            headerRange.Style.Font.FontColor = XLColor.White;

            // ── Output VAT data rows (from row 22) ───────────────────────────
            for (int r = 0; r < exportDtos.Count; r++)
            {
                var dto = exportDtos[r];
                var row = 22 + r;
                ws.Cell(row, 1).Value = r + 1;
                ws.Cell(row, 2).Value = dto.InvoiceCode;
                ws.Cell(row, 3).Value = dto.IRN ?? "—";
                ws.Cell(row, 4).Value = dto.PartyName;
                ws.Cell(row, 5).Value = dto.PartyTin ?? "—";
                ws.Cell(row, 6).Value = dto.IssueDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 7).Value = dto.TaxableAmount;
                ws.Cell(row, 7).Style.NumberFormat.Format = naira;
                ws.Cell(row, 8).Value = dto.VatAmount;
                ws.Cell(row, 8).Style.NumberFormat.Format = naira;
                ws.Cell(row, 9).Value = dto.TotalAmount;
                ws.Cell(row, 9).Style.NumberFormat.Format = naira;
                ws.Cell(row, 10).Value = dto.PaymentStatus;

                if (r % 2 == 1)
                    ws.Range(row, 1, row, columns.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            }

            // ── Output VAT totals footer ──────────────────────────────────────
            int outputFooterRow = 22 + exportDtos.Count;
            if (exportDtos.Count > 0)
            {
                ws.Cell(outputFooterRow, 6).Value = "TOTALS";
                ws.Cell(outputFooterRow, 7).Value = vatSchedule.TotalTaxableAmount;
                ws.Cell(outputFooterRow, 7).Style.NumberFormat.Format = naira;
                ws.Cell(outputFooterRow, 8).Value = vatSchedule.TotalVatAmount;
                ws.Cell(outputFooterRow, 8).Style.NumberFormat.Format = naira;
                ws.Cell(outputFooterRow, 9).Value = vatSchedule.TotalTaxableAmount + vatSchedule.TotalVatAmount;
                ws.Cell(outputFooterRow, 9).Style.NumberFormat.Format = naira;
                ws.Range(outputFooterRow, 1, outputFooterRow, columns.Length).Style.Font.Bold = true;
                ws.Range(outputFooterRow, 1, outputFooterRow, columns.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            }

            // ── Input VAT section ─────────────────────────────────────────────
            var inputCols = new[]
            {
                "S/N", "IRN", "Supplier Name", "Supplier TIN",
                "Issue Date", "Taxable Amount (₦)", "Input VAT (₦)", "Total Amount (₦)"
            };

            int inputHeaderRow = (exportDtos.Count > 0 ? outputFooterRow : outputFooterRow - 1) + 3;
            ws.Cell(inputHeaderRow - 1, 1).Value = "Section B: Input VAT — Invoices Received";
            ws.Cell(inputHeaderRow - 1, 1).Style.Font.Bold = true;
            ws.Cell(inputHeaderRow - 1, 1).Style.Font.FontSize = 11;
            ws.Range(inputHeaderRow - 1, 1, inputHeaderRow - 1, inputCols.Length).Merge();

            for (int i = 0; i < inputCols.Length; i++)
                ws.Cell(inputHeaderRow, i + 1).Value = inputCols[i];

            var inputHeaderRange = ws.Range(inputHeaderRow, 1, inputHeaderRow, inputCols.Length);
            inputHeaderRange.Style.Font.Bold = true;
            inputHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
            inputHeaderRange.Style.Font.FontColor = XLColor.White;

            int inputDataStartRow = inputHeaderRow + 1;
            for (int r = 0; r < inputExportDtos.Count; r++)
            {
                var dto = inputExportDtos[r];
                var row = inputDataStartRow + r;
                ws.Cell(row, 1).Value = r + 1;
                ws.Cell(row, 2).Value = dto.Irn;
                ws.Cell(row, 3).Value = dto.SupplierName;
                ws.Cell(row, 4).Value = dto.SupplierTin ?? "—";
                ws.Cell(row, 5).Value = dto.IssueDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 6).Value = dto.TaxableAmount;
                ws.Cell(row, 6).Style.NumberFormat.Format = naira;
                ws.Cell(row, 7).Value = dto.VatAmount;
                ws.Cell(row, 7).Style.NumberFormat.Format = naira;
                ws.Cell(row, 8).Value = dto.TotalAmount;
                ws.Cell(row, 8).Style.NumberFormat.Format = naira;

                if (r % 2 == 1)
                    ws.Range(row, 1, row, inputCols.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            }

            if (inputExportDtos.Count > 0)
            {
                int inputFooterRow = inputDataStartRow + inputExportDtos.Count;
                ws.Cell(inputFooterRow, 5).Value = "TOTALS";
                ws.Cell(inputFooterRow, 6).Value = vatSchedule.TotalInputTaxableAmount;
                ws.Cell(inputFooterRow, 6).Style.NumberFormat.Format = naira;
                ws.Cell(inputFooterRow, 7).Value = vatSchedule.TotalInputVatAmount;
                ws.Cell(inputFooterRow, 7).Style.NumberFormat.Format = naira;
                ws.Cell(inputFooterRow, 8).Value = vatSchedule.TotalInputTaxableAmount + vatSchedule.TotalInputVatAmount;
                ws.Cell(inputFooterRow, 8).Style.NumberFormat.Format = naira;
                ws.Range(inputFooterRow, 1, inputFooterRow, inputCols.Length).Style.Font.Bold = true;
                ws.Range(inputFooterRow, 1, inputFooterRow, inputCols.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            }

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 5;  // S/N stays narrow

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
