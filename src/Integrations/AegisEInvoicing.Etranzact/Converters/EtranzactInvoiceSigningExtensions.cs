using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.Etranzact.Models.Requests;

namespace AegisEInvoicing.Etranzact.Converters;

public static class EtranzactInvoiceSigningExtensions
{
    public static EtranzactInvoiceDeliveryPeriod ToEtranzactInvoiceDeliveryPeriod(this DeliveryPeriod deliveryPeriod)
    {
        return new EtranzactInvoiceDeliveryPeriod
        {
            EndDate = deliveryPeriod.EndDate,
            StartDate = deliveryPeriod.StartDate,
        };
    }

    public static EtranzactAccountingParty ToEtranzactAccountingCustomerParty(this Party party)
    {
        return new EtranzactAccountingParty
        {
            PartyName = party.Name,
            BusinessDescription = party.Description,
            Email = party.Email,
            PostalAddress = new EtranzactPostalAddress
            {
                CityName = party.Address.City,
                StreetName = party.Address.Street,
                PostalZone = party.Address.PostalCode,
                Country = party.Address.Country,
                State = party.Address.State,
                Lga = party.Address.Lga
            },
            Telephone = party.Phone,
            Tin = party.TaxIdentificationNumber.Value
        };
    }

    public static EtranzactAccountingParty ToEtranzactAccountingSupplierParty(this Business business)
    {
        return new EtranzactAccountingParty
        {
            PartyName = business.Name,
            BusinessDescription = business.Description,
            Email = business.ContactEmail,
            PostalAddress = new EtranzactPostalAddress
            {
                CityName = business.RegisteredAddress.City,
                StreetName = business.RegisteredAddress.Street,
                PostalZone = business.RegisteredAddress.PostalCode,
                Country = business.RegisteredAddress.Country,
                State = business.RegisteredAddress.State,
                Lga = business.RegisteredAddress.Lga
            },
            Telephone = business.ContactPhone,
            Tin = business.TaxIdentificationNumber.Value
        };
    }

    public static List<EtranzactBillingReference> ToEtranzactBillingReference(this List<InvoiceBillingReference> billingReferences)
    {
        var billingReference = new List<EtranzactBillingReference>();
        foreach (var item in billingReferences!)
        {
            var billingRef = new EtranzactBillingReference
            {
                Irn = item.Irn.Value,
                IssueDate = item.IssueDate
            };
            billingReference.Add(billingRef);
        }
        return billingReference;
    }

    public static List<EtranzactDocumentReference> ToEtranzactAddtionalDocumentReference(this List<InvoiceAdditionalDocumentReference> documentReferences)
    {
        var documentReference = new List<EtranzactDocumentReference>();
        foreach (var item in documentReferences!)
        {
            var docRef = new EtranzactDocumentReference
            {
                Irn = item.Irn.Value,
                IssueDate = item.IssueDate
            };
            documentReference.Add(docRef);
        }
        return documentReference;
    }

    public static EtranzactDocumentReference? ToEtranzactDispatchDocumentReference(this InvoiceDispatchDocumentReference dispatchDocumentReference)
    {
        if (dispatchDocumentReference is not null)
            return new EtranzactDocumentReference
            {
                Irn = dispatchDocumentReference.Irn.Value,
                IssueDate = dispatchDocumentReference.IssueDate
            };
        return null;
    }

    public static EtranzactDocumentReference? ToEtranzactReceiptDocumentReference(this InvoiceReceiptDocumentReference receiptDocumentReference)
    {
        if (receiptDocumentReference is not null)
            return new EtranzactDocumentReference
            {
                Irn = receiptDocumentReference.Irn.Value,
                IssueDate = receiptDocumentReference.IssueDate
            };
        return null;
    }

    public static EtranzactDocumentReference? ToEtranzactOriginatorDocumentReference(this InvoiceOriginatorDocumentReference originatorDocumentReference)
    {
        if (originatorDocumentReference is not null)
            return new EtranzactDocumentReference
            {
                Irn = originatorDocumentReference.Irn.Value,
                IssueDate = originatorDocumentReference.IssueDate
            };
        return null;
    }

    public static EtranzactDocumentReference? ToEtranzactContractDocumentReference(this InvoiceContractDocumentReference contractDocumentReference)
    {
        if (contractDocumentReference is not null)
            return new EtranzactDocumentReference
            {
                Irn = contractDocumentReference.Irn.Value,
                IssueDate = contractDocumentReference.IssueDate
            };
        return null;
    }

    public static List<EtranzactPaymentMean> ToEtranzactPaymentMeans(this PaymentMeans paymentMeans, DateOnly dueDate)
    {
        return
        [
            new() { PaymentMeansCode = paymentMeans.Code, PaymentDueDate = dueDate}
        ];
    }

    public static List<EtranzactAllowanceCharge> ToEtranzactAllowanceCharge(this List<InvoiceItem> invoiceItems)
    {
        var allowanceCharges = new List<EtranzactAllowanceCharge>();

        foreach (var item in invoiceItems!)
        {
            var totalAmount = item.Quantity * item.BusinessItem!.UnitPrice;
            if (item.DiscountFee is not null && item.DiscountFee.Amount > 0)
            {
                switch (item.DiscountFee.Code)
                {
                    case Domain.Enums.FeeStandardUnit.Percent:
                        {
                            var discountAmount = totalAmount * (item.DiscountFee.Amount / 100);
                            allowanceCharges.Add(new EtranzactAllowanceCharge
                            {
                                Amount = (double)discountAmount,
                                ChargeIndicator = false
                            });
                            break;
                        }

                    default:
                        {
                            allowanceCharges.Add(new EtranzactAllowanceCharge
                            {
                                Amount = (double)item.DiscountFee.Amount,
                                ChargeIndicator = false
                            });
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
                            allowanceCharges.Add(new EtranzactAllowanceCharge
                            {
                                Amount = (double)additionalAmount,
                                ChargeIndicator = true
                            });
                            break;
                        }

                    default:
                        {
                            allowanceCharges.Add(new EtranzactAllowanceCharge
                            {
                                Amount = (double)item.AdditionalFee.Amount,
                                ChargeIndicator = true
                            });
                            break;
                        }
                }
            }
        }
        return allowanceCharges;
    }

    public static List<EtranzactTaxTotal> ToEtranzactTaxTotal(this List<InvoiceItem> invoiceItems)
    {
        var taxSubTotals = new List<EtranzactTaxSubtotal>();

        foreach (var item in invoiceItems!)
        {
            var totalAmount = item.Quantity * item.BusinessItem!.UnitPrice;
            var discountAmount = 0.0m;
            if (item.DiscountFee != null)
            {
                if (item.DiscountFee.Code == FeeStandardUnit.Percent)
                    discountAmount = totalAmount * (item.DiscountFee.Amount / 100);
                else
                    discountAmount = item.DiscountFee.Amount;
            }

            var taxableAmount = totalAmount - discountAmount;

            if (item.BusinessItem!.TaxCategories.Count > 0)
            {
                foreach (var tc in item.BusinessItem!.TaxCategories)
                {
                    taxSubTotals.Add(new EtranzactTaxSubtotal
                    {
                        TaxableAmount = (double)taxableAmount,
                        TaxAmount = (double)tc.CalculateTax(taxableAmount),
                        TaxCategory = new EtranzactTaxCategory
                        {
                            Id = tc.Code,
                            Percent = tc.IsPercentage ? (double)tc.Percent!.Value : 0d
                        }
                    });
                }
            }
            else
            {
                taxSubTotals.Add(new EtranzactTaxSubtotal
                {
                    TaxableAmount = (double)taxableAmount,
                    TaxAmount = 0d,
                    TaxCategory = new EtranzactTaxCategory { Id = "", Percent = 0d }
                });
            }
        }

        var taxTotal = new EtranzactTaxTotal
        {
            TaxAmount = taxSubTotals.Sum(t => t.TaxAmount),
            TaxSubtotal = taxSubTotals
        };

        return [taxTotal];
    }

    public static EtranzactLegalMonetaryTotal ToEtranzactLegalMonetaryTotal(this List<InvoiceItem> invoiceItems)
    {
        decimal lineExtensionTotal = TotalAmount(invoiceItems);
        decimal taxInclusiveAmount = TotalAmount(invoiceItems) + TotalTaxAmount(invoiceItems);

        return new EtranzactLegalMonetaryTotal
        {
            LineExtensionAmount = (double)lineExtensionTotal,
            TaxExclusiveAmount = (double)lineExtensionTotal,
            TaxInclusiveAmount = (double)taxInclusiveAmount,
            PayableAmount = (double)taxInclusiveAmount
        };
    }

    public static List<EtranzactInvoiceLine> ToEtranzactInvoiceLine(this List<InvoiceItem> invoiceItems, string currency)
    {
        var invioceLines = new List<EtranzactInvoiceLine>();
        foreach (var item in invoiceItems)
        {
            var invoiceLine = new EtranzactInvoiceLine
            {
                HsnCode = item.BusinessItem!.ItemType == ItemType.Goods ? item.BusinessItem.ServiceCode!.Code : null,
                ProductCategory = item.BusinessItem!.ItemType == ItemType.Goods ? item.BusinessItem.ServiceCode!.Name : null,
                IsicCode = item.BusinessItem!.ItemType == ItemType.Service ? item.BusinessItem.ServiceCode!.Code : null,
                ServiceCategory = item.BusinessItem!.ItemType == ItemType.Service ? item.BusinessItem.ServiceCode!.Name : null,
                InvoicedQuantity = (int)item.Quantity,
                Item = new EtranzactItem
                {
                    Name = item.BusinessItem!.Name,
                    Description = item.BusinessItem!.ItemDescription
                },
                LineExtensionAmount = (double)(item.Quantity * item.BusinessItem!.UnitPrice),
                Price = new EtranzactPrice
                {
                    BaseQuantity = 1,
                    PriceUnit = $"{currency} per 1",
                    PriceAmount = (double)item.BusinessItem!.UnitPrice
                }
            };

            if (item.DiscountFee != null)
            {
                if (item.DiscountFee.Code == FeeStandardUnit.Percent)
                {
                    invoiceLine.DiscountRate = (double)item.DiscountFee.Amount;

                    // Calculate DiscountAmount
                    var invoiceItemTotal = item.Quantity * item.BusinessItem!.UnitPrice;
                    invoiceLine.DiscountAmount = (double)(invoiceItemTotal * (item.DiscountFee.Amount / 100));
                }
                else
                {
                    invoiceLine.DiscountAmount = (double)item.DiscountFee.Amount;

                    // Calculate DiscountRate
                    var invoiceItemTotal = item.Quantity * item.BusinessItem!.UnitPrice;
                    invoiceLine.DiscountRate = (double)((item.DiscountFee.Amount / invoiceItemTotal) * 100);
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
            var invoiceItemTotal = item.Quantity * item.BusinessItem!.UnitPrice;
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
            var invoiceItemTotal = item.Quantity * item.BusinessItem!.UnitPrice;
            var discountAmount = 0.0m;
            if (item.DiscountFee != null)
            {
                if (item.DiscountFee.Code == FeeStandardUnit.Percent)
                    discountAmount = invoiceItemTotal * (item.DiscountFee.Amount / 100);
                else
                    discountAmount = item.DiscountFee.Amount;
            }
            var actualItemTotal = invoiceItemTotal - discountAmount;

            totalTaxAmount += item.BusinessItem!.TaxCategories.Sum(tc => tc.CalculateTax(actualItemTotal));
        }
        return totalTaxAmount;
    }
}
