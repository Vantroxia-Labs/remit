namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceStatisticsDto(
    int TotalInvoices,
    int DraftInvoices,
    int SubmittedInvoices,
    int ApprovedInvoices,
    int RejectedInvoices,
    int InvoicesThisMonth,
    int InvoicesThisWeek);