using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

public class InvoiceApprovalHistory : AuditableEntity
{
    public Guid InvoiceId { get; private set; }
    public InvoiceStatus InvoiceStatus { get; private set; }
    public string Comments { get; private set; } = string.Empty;
    
    public Invoice Invoice { get; private set; } = null!;
    public UserManagement.User CreatedByUser { get; private set; } = null!;

    private InvoiceApprovalHistory() { }

    public static InvoiceApprovalHistory Create(
        Guid invoiceId,
        InvoiceStatus invoiceStatus,
        string comments)
    {
        var invoiceApprovalHistory = new InvoiceApprovalHistory
        {
            InvoiceId = invoiceId,
            InvoiceStatus = invoiceStatus,
            Comments = comments
        };
        return invoiceApprovalHistory;
    }
}
