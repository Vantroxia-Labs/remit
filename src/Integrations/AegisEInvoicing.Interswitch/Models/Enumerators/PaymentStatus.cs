using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Interswitch.Models.Enumerators;

public enum PaymentStatus
{
    [Display(Name = "PENDING")]
    Pending,
    [Display(Name = "PAID")]
    Paid,
    [Display(Name = "CANCELLED")]
    Cancelled,
    [Display(Name = "FAILED")]
    Failed,
    Rejected
}