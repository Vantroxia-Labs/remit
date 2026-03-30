using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.Interswitch.Models.Requests.SignInvoice;
using DocumentReference = AegisEInvoicing.Interswitch.Models.Requests.SignInvoice.DocumentReference;
using PostalAddress = AegisEInvoicing.Interswitch.Models.Requests.SignInvoice.PostalAddress;
using TaxCategory = AegisEInvoicing.Interswitch.Models.Requests.SignInvoice.TaxCategory;

namespace AegisEInvoicing.Application.Common.Extensions;

public static class FirsInvoiceSigningExtensions
{
    public static InvoiceDeliveryPeriod ToSigningInvoiceDeliveryPeriod(this DeliveryPeriod deliveryPeriod)
    {
        return new InvoiceDeliveryPeriod
        {
            EndDate = deliveryPeriod.EndDate,
            StartDate = deliveryPeriod.StartDate,
        };
    }

    public static AccountingCustomerParty ToSigningAccountingCustomerParty(this Party party)
    {
        return new AccountingCustomerParty
        {
            PartyName = party.Name,
            BusinessDescription = party.Description,
            Email = party.Email,
            PostalAddress = new PostalAddress
            {
                CityName = party.Address.City,
                StreetName = party.Address.Street,
                PostalZone = party.Address.PostalCode,
                Country = party.Address.Country
            },
            Telephone = party.Phone,
            Tin = party.TaxIdentificationNumber.Value
        };
    }

    public static AccountingSupplierParty ToSigningAccountingSupplierParty(this Business business)
    {
        return new AccountingSupplierParty
        {
            PartyName = business.Name,
            BusinessDescription = business.Description,
            Email = business.ContactEmail,
            PostalAddress = new PostalAddress
            {
                CityName = business.RegisteredAddress.City,
                StreetName = business.RegisteredAddress.Street,
                PostalZone = business.RegisteredAddress.PostalCode,
                Country = business.RegisteredAddress.Country
            },
            Telephone = business.ContactPhone,
            Tin = business.TaxIdentificationNumber.Value
        };
    }
    

    public static List<BillingReference> ToSigningBillingReference(this List<InvoiceBillingReference> billingReferences)
    {
        var billingReference = new List<BillingReference>();

        foreach (var item in billingReferences!)
        {
            var billingRef = new BillingReference
            {
                Irn = item.Irn.Value,
                IssueDate = item.IssueDate
            };
            billingReference.Add(billingRef);
        }
        return billingReference;
    }

    public static List<DocumentReference> ToSigningAddtionalDocumentReference(this List<InvoiceAdditionalDocumentReference> documentReferences)
    {
        var documentReference = new List<DocumentReference>();

        foreach (var item in documentReferences!)
        {
            var billingRef = new DocumentReference
            {
                Irn = item.Irn.Value,
                IssueDate = item.IssueDate
            };
            documentReference.Add(billingRef);
        }
        return documentReference;
    }

    public static DispatchDocumentReference? ToSigningDispatchDocumentReference(this InvoiceDispatchDocumentReference dispatchDocumentReference)
    {
        if (dispatchDocumentReference is not null)
            return new DispatchDocumentReference
            {
                 Irn = dispatchDocumentReference.Irn.Value,
                 IssueDate = dispatchDocumentReference.IssueDate
            };

        return null;
    }

    public static ReceiptDocumentReference? ToSigningReceiptDocumentReference(this InvoiceReceiptDocumentReference receiptDocumentReference)
    {
        if (receiptDocumentReference is not null)
            return new ReceiptDocumentReference
            {
                Irn = receiptDocumentReference.Irn.Value,
                IssueDate = receiptDocumentReference.IssueDate
            };

        return null;
    }

    public static OriginatorDocumentReference? ToSigningOriginatorDocumentReference(this InvoiceOriginatorDocumentReference originatorDocumentReference)
    {
        if (originatorDocumentReference is not null)
            return new OriginatorDocumentReference
            {
                Irn = originatorDocumentReference.Irn.Value,
                IssueDate = originatorDocumentReference.IssueDate
            };

        return null;
    }

    public static ContractDocumentReference? ToSigningContractDocumentReference(this InvoiceContractDocumentReference contractDocumentReference)
    {
        if (contractDocumentReference is not null)
            return new ContractDocumentReference
            {
                Irn = contractDocumentReference.Irn.Value,
                IssueDate = contractDocumentReference.IssueDate
            };

        return null;
    }

    public static List<PaymentMean> ToSigningPaymentMeans(this PaymentMeans paymentMeans, DateOnly dueDate)
    {
        return
        [
            new() { PaymentMeansCode = paymentMeans.Code, PaymentDueDate = dueDate}
        ];
    }

    public static List<AllowanceCharge> ToSigningAllowanceCharge(this List<InvoiceItem> invoiceItems)
    {
        var allowanceCharges = new List<AllowanceCharge>();

        foreach (var item in invoiceItems!)
        {
            var totalAmount = item.Quantity * item.BusinessItem.UnitPrice;
            if (item.DiscountFee is not null && item.DiscountFee.Amount > 0)
            {
                switch (item.DiscountFee.Code)
                {
                    case Domain.Enums.FeeStandardUnit.Percent:
                        {
                            var discountAmount = totalAmount * (item.DiscountFee.Amount / 100);
                            var allowCharge = new AllowanceCharge
                            {
                                Amount = discountAmount,
                                ChargeIndicator = false
                            };
                            allowanceCharges.Add(allowCharge);
                            break;
                        }

                    default:
                        {
                            var allowCharge = new AllowanceCharge
                            {
                                Amount = item.DiscountFee.Amount,
                                ChargeIndicator = false
                            };
                            allowanceCharges.Add(allowCharge);
                            break;
                        }
                }
            }

            if (item.AdditionalFee is not null && item.AdditionalFee.Amount > 0)
            {
                switch (item.AdditionalFee.Code)
                {
                    case Domain.Enums.FeeStandardUnit.Percent:
                        {
                            var additionalAmount = totalAmount * (item.AdditionalFee.Amount / 100);
                            var allowCharge = new AllowanceCharge
                            {
                                Amount = additionalAmount,
                                ChargeIndicator = true
                            };
                            allowanceCharges.Add(allowCharge);
                            break;
                        }

                    default:
                        {
                            var allowCharge = new AllowanceCharge
                            {
                                Amount = item.AdditionalFee.Amount,
                                ChargeIndicator = true
                            };
                            allowanceCharges.Add(allowCharge);
                            break;
                        }
                }
            }
        }
        return allowanceCharges;
    }

    public static List<TaxTotal> ToSigningTaxTotal(this List<InvoiceItem> invoiceItems)
    {

        var taxSubTotals = new List<TaxSubtotal>();

        foreach (var item in invoiceItems!)
        {

            var totalAmount = item.Quantity * item.BusinessItem.UnitPrice;
            var discountAmount = 0.0m;
            if (item.DiscountFee != null)
            {
                if (item.DiscountFee.Code == FeeStandardUnit.Percent)
                    discountAmount = totalAmount * (item.DiscountFee.Amount / 100);
                else
                    discountAmount = item.DiscountFee.Amount;
            }

            var taxSubTotal = new TaxSubtotal
            {
                TaxableAmount = totalAmount - discountAmount,
                TaxAmount = (totalAmount - discountAmount) * (item.BusinessItem.TaxCategory.Percent / 100),
                TaxCategory = new TaxCategory
                {
                    Id = item.BusinessItem.TaxCategory.Name,
                    Percent = item.BusinessItem.TaxCategory.Percent
                }
            };
            taxSubTotals.Add(taxSubTotal);
        }

        var taxTotal = new TaxTotal
        {
            TaxAmount = taxSubTotals.Sum(t => t.TaxAmount),
            TaxSubtotal = taxSubTotals
        };

        return [taxTotal];

        //var taxTotals = new List<TaxTotal>();
        //foreach (var item in invoiceItems!)
        //{
        //    var totalAmount = item.Quantity * item.BusinessItem.UnitPrice;
        //    var taxTotal = new TaxTotal
        //    {
        //        TaxAmount = totalAmount * (item.BusinessItem.TaxCategory.Percent / 100),
        //        TaxSubtotal =
        //        [
        //            new()
        //            {
        //                TaxableAmount = item.BusinessItem.UnitPrice,
        //                TaxAmount = item.BusinessItem.UnitPrice * (item.BusinessItem.TaxCategory.Percent / 100),
        //                TaxCategory = new TaxCategory
        //                {
        //                     Id = item.BusinessItem.TaxCategory.Name,
        //                     Percent = item.BusinessItem.TaxCategory.Percent
        //                }
        //            }
        //        ]
        //    };
        //    taxTotals.Add(taxTotal);
        //}
        //return taxTotals;
    }

    public static LegalMonetaryTotal ToSigningLegalMonetaryTotal(this List<InvoiceItem> invoiceItems)
    {
        decimal lineExtensionTotal = TotalAmount(invoiceItems);
        decimal taxInclusiveAmount = TotalAmount(invoiceItems) + TotalTaxAmount(invoiceItems);

        return new LegalMonetaryTotal
        {
            LineExtensionAmount = lineExtensionTotal,
            TaxExclusiveAmount = lineExtensionTotal,
            TaxInclusiveAmount = taxInclusiveAmount,
            PayableAmount = taxInclusiveAmount
        };
    }

    public static List<InvoiceLine> ToSigningInvoiceLine(this List<InvoiceItem> invoiceItems, string currency)
    {
        var invioceLines = new List<InvoiceLine>();
        foreach (var item in invoiceItems)
        {
            var invoiceLine = new InvoiceLine
            {
                HsnCode = item.BusinessItem.ServiceCode.Code,
                ProductCategory = item.BusinessItem.ItemCategory.Name,
                //DiscountRate = item.DiscountFee!.Amount,
                //DiscountAmount
                //FeeRate
                //FeeAmount
                InvoicedQuantity = item.Quantity,
                Item = new Item
                {
                    Name = item.BusinessItem.Name,
                    Description = item.BusinessItem.ItemDescription
                },
                LineExtensionAmount = item.Quantity * item.BusinessItem.UnitPrice,
                Price = new Price
                {
                    BaseQuantity = 1,
                    PriceUnit = $"{currency} per 1",
                    PriceAmount = item.BusinessItem.UnitPrice
                }
            };

            if (item.DiscountFee != null)
            {
                if (item.DiscountFee.Code == FeeStandardUnit.Percent)
                {
                    invoiceLine.DiscountRate = item.DiscountFee.Amount;

                    // Calculate DiscountAmount
                    var invoiceItemTotal = item.Quantity * item.BusinessItem.UnitPrice;
                    invoiceLine.DiscountAmount = invoiceItemTotal * (item.DiscountFee.Amount / 100);
                }
                else
                {
                    invoiceLine.DiscountAmount = item.DiscountFee.Amount;

                    // Calculate DiscountRate
                    var invoiceItemTotal = item.Quantity * item.BusinessItem.UnitPrice;
                    invoiceLine.DiscountRate = (item.DiscountFee.Amount / invoiceItemTotal) * 100;
                }
            }

            invioceLines.Add(invoiceLine);
        }
        return invioceLines;
    }


    private static decimal TotalAmount(List<InvoiceItem> invoiceItems)
    {
        decimal totalAmount = 0;
        foreach (var item in invoiceItems!)
        {
            var invoiceItemTotal = item.Quantity * item.BusinessItem.UnitPrice;
            var discountAmount = 0.0m;
            if (item.DiscountFee != null)
            {
                if (item.DiscountFee.Code == FeeStandardUnit.Percent)
                    discountAmount = invoiceItemTotal * (item.DiscountFee.Amount / 100);
                else
                    discountAmount = item.DiscountFee.Amount;
            }

            var actualItemTotal = invoiceItemTotal - discountAmount;
            totalAmount += actualItemTotal;
        }
        return totalAmount;
    }

    private static decimal TotalTaxAmount(List<InvoiceItem> invoiceItems)
    {
        decimal totalTaxAmount = 0;
        foreach (var item in invoiceItems!)
        {
            var invoiceItemTotal = item.Quantity * item.BusinessItem.UnitPrice;
            var discountAmount = 0.0m;
            if (item.DiscountFee != null)
            {
                if (item.DiscountFee.Code == FeeStandardUnit.Percent)
                    discountAmount = invoiceItemTotal * (item.DiscountFee.Amount / 100);
                else
                    discountAmount = item.DiscountFee.Amount;
            }
            var actualItemTotal = invoiceItemTotal - discountAmount;

            var tax = item.BusinessItem.TaxCategory.Percent;
            var taxAmount = actualItemTotal * (tax / 100);

            totalTaxAmount += taxAmount;
        }
        return totalTaxAmount;
    }
}