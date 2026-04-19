using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;

public enum PaymentStatus
{
    [Display(Name = "PENDING")]
    Pending,
    [Display(Name = "PAID")]
    Paid,
    [Display(Name = "REJECTED")]
    Rejected,
    [Display(Name = "CANCELLED")]
    Cancelled,
    [Display(Name = "DISMISSED")]
    Dismissed
}