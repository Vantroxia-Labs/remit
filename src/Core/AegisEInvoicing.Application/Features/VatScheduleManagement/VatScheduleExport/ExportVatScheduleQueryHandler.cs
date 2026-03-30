using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.VatScheduleExport.Dto;
using AutoMapper;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (vatSchedule == null)
            {
                // Or throw a NotFoundException
                return Array.Empty<byte>();
            }

            var exportDtos = _mapper.Map<List<VatScheduleItemExportDto>>(vatSchedule.Items);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("VAT Schedule");

            // --- Header ---
            var header = worksheet.Cell("A1");
            header.Value = $"VAT Schedule for {vatSchedule.MonthName} {vatSchedule.Year}";
            header.Style.Font.Bold = true;
            header.Style.Font.FontSize = 16;
            worksheet.Range("A1:H1").Merge().AddToNamed("Titles");

            // --- Summary ---
            worksheet.Cell("A3").Value = "Total Invoices:";
            worksheet.Cell("B3").Value = vatSchedule.TotalInvoiceCount;
            worksheet.Cell("A4").Value = "Total Taxable Amount:";
            worksheet.Cell("B4").Value = vatSchedule.TotalTaxableAmount;
            worksheet.Cell("B4").Style.NumberFormat.Format = "_(\"₦\"* #,##0.00_);_(\"₦\"* (#,##0.00);_(\"₦\"* \"-\"??_);_(@_)";
            worksheet.Cell("A5").Value = "Total VAT Amount:";
            worksheet.Cell("B5").Value = vatSchedule.TotalVatAmount;
            worksheet.Cell("B5").Style.NumberFormat.Format = "_(\"₦\"* #,##0.00_);_(\"₦\"* (#,##0.00);_(\"₦\"* \"-\"??_);_(@_)";
            worksheet.Range("A3:A5").Style.Font.Bold = true;


            // --- Table Header ---
            var tableHeader = worksheet.Cell("A7");
            var properties = new[]
            {
                "Invoice Code", "IRN", "Party Name", "Party TIN", "Issue Date",
                "Taxable Amount", "VAT Amount", "Total Amount", "Payment Status"
            };

            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cell(7, i + 1).Value = properties[i];
            }
            worksheet.Range(7, 1, 7, properties.Length).Style.Font.Bold = true;
            worksheet.Range(7, 1, 7, properties.Length).Style.Fill.BackgroundColor = XLColor.LightGray;


            // --- Table Data ---
            if (exportDtos.Any())
            {
                var table = worksheet.Cell("A8").InsertTable(exportDtos);
                table.Theme = XLTableTheme.TableStyleLight9;

                // Format columns
                table.Column("F").Style.NumberFormat.Format = "_(\"₦\"* #,##0.00_);_(\"₦\"* (#,##0.00);_(\"₦\"* \"-\"??_);_(@_)";
                table.Column("G").Style.NumberFormat.Format = "_(\"₦\"* #,##0.00_);_(\"₦\"* (#,##0.00);_(\"₦\"* \"-\"??_);_(@_)";
                table.Column("H").Style.NumberFormat.Format = "_(\"₦\"* #,##0.00_);_(\"₦\"* (#,##0.00);_(\"₦\"* \"-\"??_);_(@_)";
                table.Column("E").Style.DateFormat.Format = "yyyy-mm-dd";
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
