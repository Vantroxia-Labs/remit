using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.BlueBridge.Models.Requests;

namespace AegisEInvoicing.BlueBridge.Converters;

public static class BlueBridgeInvoiceSigningExtensions
{
    public static BlueBridgeInvoiceDeliveryPeriod ToBlueBridgeInvoiceDeliveryPeriod(this DeliveryPeriod deliveryPeriod)
    {
        return new BlueBridgeInvoiceDeliveryPeriod
        {
            EndDate = deliveryPeriod.EndDate,
            StartDate = deliveryPeriod.StartDate,
        };
    }

    public static BlueBridgeAccountingParty ToBlueBridgeAccountingCustomerParty(this Party party)
    {
        return new BlueBridgeAccountingParty
        {
            PartyName = party.Name,
            BusinessDescription = party.Description,
            Email = party.Email,
            PostalAddress = new BlueBridgePostalAddress
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

    public static BlueBridgeAccountingParty ToBlueBridgeAccountingSupplierParty(this Business business)
    {
        return new BlueBridgeAccountingParty
        {
            PartyName = business.Name,
            BusinessDescription = business.Description,
            Email = business.ContactEmail,
            PostalAddress = new BlueBridgePostalAddress
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

    public static List<BlueBridgeBillingReference> ToBlueBridgeBillingReference(this List<InvoiceBillingReference> billingReferences)
    {
        var billingReference = new List<BlueBridgeBillingReference>();
        foreach (var item in billingReferences!)
        {
            var billingRef = new BlueBridgeBillingReference
            {
                Irn = item.Irn.Value,
                IssueDate = item.IssueDate
            };
            billingReference.Add(billingRef);
        }
        return billingReference;
    }

    public static List<BlueBridgeDocumentReference> ToBlueBridgeAddtionalDocumentReference(this List<InvoiceAdditionalDocumentReference> documentReferences)
    {
        var documentReference = new List<BlueBridgeDocumentReference>();
        foreach (var item in documentReferences!)
        {
            var docRef = new BlueBridgeDocumentReference
            {
                Irn = item.Irn.Value,
                IssueDate = item.IssueDate
            };
            documentReference.Add(docRef);
        }
        return documentReference;
    }

    public static BlueBridgeDocumentReference? ToBlueBridgeDispatchDocumentReference(this InvoiceDispatchDocumentReference dispatchDocumentReference)
    {
        if (dispatchDocumentReference is not null)
            return new BlueBridgeDocumentReference
            {
                Irn = dispatchDocumentReference.Irn.Value,
                IssueDate = dispatchDocumentReference.IssueDate
            };
        return null;
    }

    public static BlueBridgeDocumentReference? ToBlueBridgeReceiptDocumentReference(this InvoiceReceiptDocumentReference receiptDocumentReference)
    {
        if (receiptDocumentReference is not null)
            return new BlueBridgeDocumentReference
            {
                Irn = receiptDocumentReference.Irn.Value,
                IssueDate = receiptDocumentReference.IssueDate
            };
        return null;
    }

    public static BlueBridgeDocumentReference? ToBlueBridgeOriginatorDocumentReference(this InvoiceOriginatorDocumentReference originatorDocumentReference)
    {
        if (originatorDocumentReference is not null)
            return new BlueBridgeDocumentReference
            {
                Irn = originatorDocumentReference.Irn.Value,
                IssueDate = originatorDocumentReference.IssueDate
            };
        return null;
    }

    public static BlueBridgeDocumentReference? ToBlueBridgeContractDocumentReference(this InvoiceContractDocumentReference contractDocumentReference)
    {
        if (contractDocumentReference is not null)
            return new BlueBridgeDocumentReference
            {
                Irn = contractDocumentReference.Irn.Value,
                IssueDate = contractDocumentReference.IssueDate
            };
        return null;
    }

    public static List<BlueBridgePaymentMean> ToBlueBridgePaymentMeans(this PaymentMeans paymentMeans, DateOnly dueDate)
    {
        return
        [
            new() { PaymentMeansCode = paymentMeans.Code, PaymentDueDate = dueDate}
        ];
    }

    public static List<BlueBridgeAllowanceCharge> ToBlueBridgeAllowanceCharge(this List<InvoiceItem> invoiceItems)
    {
        var allowanceCharges = new List<BlueBridgeAllowanceCharge>();

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
                            allowanceCharges.Add(new BlueBridgeAllowanceCharge
                            {
                                Amount = (double)discountAmount,
                                ChargeIndicator = false
                            });
                            break;
                        }

                    default:
                        {
                            allowanceCharges.Add(new BlueBridgeAllowanceCharge
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
                            allowanceCharges.Add(new BlueBridgeAllowanceCharge
                            {
                                Amount = (double)additionalAmount,
                                ChargeIndicator = true
                            });
                            break;
                        }

                    default:
                        {
                            allowanceCharges.Add(new BlueBridgeAllowanceCharge
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

    public static List<BlueBridgeTaxTotal> ToBlueBridgeTaxTotal(this List<InvoiceItem> invoiceItems)
    {
        var taxSubTotals = new List<BlueBridgeTaxSubtotal>();

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
                    taxSubTotals.Add(new BlueBridgeTaxSubtotal
                    {
                        TaxableAmount = (double)taxableAmount,
                        TaxAmount = (double)tc.CalculateTax(taxableAmount),
                        TaxCategory = new BlueBridgeTaxCategory
                        {
                            Id = tc.Code,
                            Percent = tc.IsPercentage ? (double)tc.Percent!.Value : 0d
                        }
                    });
                }
            }
            else
            {
                taxSubTotals.Add(new BlueBridgeTaxSubtotal
                {
                    TaxableAmount = (double)taxableAmount,
                    TaxAmount = 0d,
                    TaxCategory = new BlueBridgeTaxCategory { Id = "", Percent = 0d }
                });
            }
        }

        var taxTotal = new BlueBridgeTaxTotal
        {
            TaxAmount = taxSubTotals.Sum(t => t.TaxAmount),
            TaxSubtotal = taxSubTotals
        };

        return [taxTotal];
    }

    public static BlueBridgeLegalMonetaryTotal ToBlueBridgeLegalMonetaryTotal(this List<InvoiceItem> invoiceItems)
    {
        decimal lineExtensionTotal = TotalAmount(invoiceItems);
        decimal taxInclusiveAmount = TotalAmount(invoiceItems) + TotalTaxAmount(invoiceItems);

        return new BlueBridgeLegalMonetaryTotal
        {
            LineExtensionAmount = (double)lineExtensionTotal,
            TaxExclusiveAmount = (double)lineExtensionTotal,
            TaxInclusiveAmount = (double)taxInclusiveAmount,
            PayableAmount = (double)taxInclusiveAmount
        };
    }

    public static List<BlueBridgeInvoiceLine> ToBlueBridgeInvoiceLine(this List<InvoiceItem> invoiceItems, string currency)
    {
        var invioceLines = new List<BlueBridgeInvoiceLine>();
        foreach (var item in invoiceItems)
        {
            var invoiceLine = new BlueBridgeInvoiceLine
            {
                HsnCode = item.BusinessItem!.ItemType == ItemType.Goods ? item.BusinessItem.ServiceCode!.Code : null,
                ProductCategory = item.BusinessItem!.ItemType == ItemType.Goods ? item.BusinessItem.ServiceCode!.Name : null,
                IsicCode = item.BusinessItem!.ItemType == ItemType.Service ? item.BusinessItem.ServiceCode!.Code : null,
                ServiceCategory = item.BusinessItem!.ItemType == ItemType.Service ? item.BusinessItem.ServiceCode!.Name : null,
                InvoicedQuantity = (int)item.Quantity,
                Item = new BlueBridgeItem
                {
                    Name = item.BusinessItem!.Name,
                    Description = item.BusinessItem!.ItemDescription
                },
                LineExtensionAmount = (double)(item.Quantity * item.BusinessItem!.UnitPrice),
                Price = new BlueBridgePrice
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
