using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceWithParty;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.ERP.API.Models;
using System.Text.RegularExpressions;

namespace AegisEInvoicing.ERP.API.Extensions;

public static class InvoiceMappingExtensions
{
    public static CreateInvoiceWithPartyCommand MapToCreateInvoiceWithPartyCommand(this CreateInvoiceRequest request)
    {
        return new CreateInvoiceWithPartyCommand
        {
            BusinessId = request.AegisBusinessId,
            IssueDate = request.IssueDate,
            IssueTime = request.IssueTime,
            InvoiceType = AegisEInvoicing.Domain.ValueObjects.InvoiceType.Create(request.InvoiceType.Name, request.InvoiceType.Code),
            Currency = AegisEInvoicing.Domain.ValueObjects.Currency.Create(request.Currency.Name, request.Currency.Code),
            DeliveryPeriod = AegisEInvoicing.Domain.ValueObjects.DeliveryPeriod.Create(request.DeliveryPeriod.StartDate, request.DeliveryPeriod.EndDate),
            DueDate = request.DueDate,
            PaymentMeans = AegisEInvoicing.Domain.ValueObjects.PaymentMeans.Create(request.PaymentMeans.Code, request.PaymentMeans.Name),
            Note = request.Note,
            PaymentReference = request.PaymentReference,
            PaymentTerms = request.PaymentTerms,
            Party = new CreatePartyDto(
                request.Party.Name,
                request.Party.Phone,
                request.Party.Email,
                request.Party.TaxIdentificationNumber,
                request.Party.Description,
                new CreateAddressDto(
                    request.Party.Address.Street,
                    request.Party.Address.City,
                    request.Party.Address.State,
                    request.Party.Address.Country,
                    request.Party.Address.PostalCode
                )
            ),
            InvoiceItems = request.InvoiceItems.Select(item => new AegisEInvoicing.Application.Features.InvoiceManagement.DTOs.CreateInvoiceItemDto
            {
                BusinessItemId = Guid.NewGuid(), // This should ideally come from the item mapping or be looked up
                Quantity = item.Quantity,
                DiscountFee = item.DiscountFee,
                AdditionalFee = item.AdditionalFee
            }).ToList()
        };
    }

    public static CreateInvoiceCommand MapToCreateInvoiceCommand(this CreateInvoiceRequest request)
    {
        return new CreateInvoiceCommand
        {
            PartyId = Guid.Empty, // Will be set by the command handler after party creation
            IssueDate = request.IssueDate,
            InvoiceType = AegisEInvoicing.Domain.ValueObjects.InvoiceType.Create(request.InvoiceType.Name, request.InvoiceType.Code),
            Currency = AegisEInvoicing.Domain.ValueObjects.Currency.Create(request.Currency.Name, request.Currency.Code),
            DeliveryPeriod = AegisEInvoicing.Domain.ValueObjects.DeliveryPeriod.Create(request.DeliveryPeriod.StartDate, request.DeliveryPeriod.EndDate),
            DueDate = request.DueDate,
            PaymentMeans = AegisEInvoicing.Domain.ValueObjects.PaymentMeans.Create(request.PaymentMeans.Code, request.PaymentMeans.Name),
            Note = request.Note,
            PaymentReference = request.PaymentReference,
            PaymentTerms = request.PaymentTerms,
            InvoiceItems = request.InvoiceItems.Select(item => new AegisEInvoicing.Application.Features.InvoiceManagement.DTOs.CreateInvoiceItemDto
            {
                BusinessItemId = Guid.NewGuid(), // This should ideally come from the item mapping or be looked up
                Quantity = item.Quantity,
                DiscountFee = item.DiscountFee,
                AdditionalFee = item.AdditionalFee
            }).ToList()
        };
    }

    public static CreateFIRSInvoiceCommand MapToCreateFIRSInvoiceCommand(this CreateInvoiceRequest request)
    {
        var varOcg = new Regex("[^a-zA-Z0-9]");

        // Parse InvoiceKind if provided
        AegisEInvoicing.Domain.Enums.InvoiceKind? invoiceKind = null;
        if (!string.IsNullOrWhiteSpace(request.InvoiceKind) &&
            Enum.TryParse<AegisEInvoicing.Domain.Enums.InvoiceKind>(request.InvoiceKind, true, out var parsedKind))
        {
            invoiceKind = parsedKind;
        }

        return new CreateFIRSInvoiceCommand
        {
            BusinessId = request.AegisBusinessId,
            InvoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber)
                          ? string.Empty
                          : varOcg.Replace(request.InvoiceNumber, ""),
            IssueDate = request.IssueDate,
            IssueTime = request.IssueTime,
            InvoiceType = AegisEInvoicing.Domain.ValueObjects.InvoiceType.Create(request.InvoiceType.Name, request.InvoiceType.Code),
            InvoiceKind = invoiceKind,
            Currency = AegisEInvoicing.Domain.ValueObjects.Currency.Create(request.Currency.Name, request.Currency.Code),
            DeliveryPeriod = AegisEInvoicing.Domain.ValueObjects.DeliveryPeriod.Create(request.DeliveryPeriod.StartDate, request.DeliveryPeriod.EndDate),
            DueDate = request.DueDate,
            PaymentMeans = AegisEInvoicing.Domain.ValueObjects.PaymentMeans.Create(request.PaymentMeans.Code, request.PaymentMeans.Name),
            Note = request.Note,
            PaymentReference = request.PaymentReference,
            PaymentTerms = request.PaymentTerms,
            Party = new CreatePartyDto(
                request.Party.Name,
                request.Party.Phone,
                request.Party.Email,
                request.Party.TaxIdentificationNumber,
                request.Party.Description,
                new CreateAddressDto(
                    request.Party.Address.Street,
                    request.Party.Address.City,
                    request.Party.Address.State,
                    request.Party.Address.Country,
                    request.Party.Address.PostalCode
                )
            ),
            InvoiceSource = AegisEInvoicing.Domain.Enums.InvoiceSource.ERP,
            InvoiceItems = request.InvoiceItems.Select(item => new AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice.InvoiceItemRequest
            {
                Name = item.Name,
                ItemDescription = item.ItemDescription,
                ItemCategory = item.ItemCategory,
                ServiceCode = new AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice.ServiceCodeRequest
                {
                    Code = item.ServiceCode.Code,
                    Name = item.ServiceCode.Name
                },
                TaxCategories = item.TaxCategories?.Select(tc => new AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice.TaxCategoryRequest
                {
                    Name = tc.Name,
                    IsPercentage = tc.IsPercentage,
                    Percent = tc.Percent,
                    FlatAmount = tc.FlatAmount
                }).ToList() ?? [],
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                DiscountFee = item.DiscountFee,
                AdditionalFee = item.AdditionalFee
            }).ToList(),
            BillingReferences = request.BillingReferences,
            DispatchDocumentReference = request.DispatchDocumentReference,
            ReceiptDocumentReference = request.ReceiptDocumentReference,
            OriginatorDocumentReference = request.OriginatorDocumentReference,
            ContractDocumentReference = request.ContractDocumentReference,
            AdditionalDocumentReferences = request.AdditionalDocumentReferences
        };
    }
}
