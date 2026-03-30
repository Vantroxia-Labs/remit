namespace AegisEInvoicing.ERP.API.Models;

public record GenerateIrnRequest(string InvoiceNumber, DateOnly IssueDate);
