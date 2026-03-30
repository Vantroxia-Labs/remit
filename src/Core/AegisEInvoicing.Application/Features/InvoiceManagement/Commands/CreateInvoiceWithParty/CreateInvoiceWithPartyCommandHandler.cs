using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateParty;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceWithParty;

public class CreateInvoiceWithPartyCommandHandler : IRequestHandler<CreateInvoiceWithPartyCommand, CreateInvoiceWithPartyResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateInvoiceWithPartyCommandHandler> _logger;
    private readonly IMediator _mediator;

    public CreateInvoiceWithPartyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateInvoiceWithPartyCommandHandler> logger,
        IMediator mediator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<CreateInvoiceWithPartyResult> Handle(CreateInvoiceWithPartyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate Business ID from request against current user's business
            if (!_currentUserService.BusinessId.HasValue)
            {
                return new CreateInvoiceWithPartyResult
                {
                    Success = false,
                    Message = "User not authenticated or no business associated"
                };
            }

            if (request.BusinessId == Guid.Empty || request.BusinessId != _currentUserService.BusinessId.Value)
            {
                return new CreateInvoiceWithPartyResult
                {
                    Success = false,
                    Message = "Invalid BusinessId or access denied to this business"
                };
            }

            // Validate that the business exists
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId, cancellationToken);

            if (business is null)
            {
                return new CreateInvoiceWithPartyResult
                {
                    Success = false,
                    Message = "Business not found"
                };
            }

            if (string.IsNullOrWhiteSpace(business.ServiceId) || business.ServiceId.Length != 8)
            {
                return new CreateInvoiceWithPartyResult
                {
                    Success = false,
                    Message = "Business does not have a valid Service ID. Please complete registration first."
                };
            }

            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return new CreateInvoiceWithPartyResult
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Step 1: Check if party already exists or create new one
            _logger.LogInformation("Checking for existing party with email {Email} for business {BusinessId}", 
                request.Party.Email, request.BusinessId);
            
            // Look for existing party by email within the same business
            var existingParty = await _context.Parties
                .FirstOrDefaultAsync(p => p.TaxIdentificationNumber.Value == request.Party.TaxIdentificationNumber && 
                                         p.BusinessID == request.BusinessId &&
                                         !p.IsDeleted, cancellationToken);

            Guid partyId;
            bool partyCreated = false;

            if (existingParty != null)
            {
                _logger.LogInformation("Found existing party {PartyId} with email {Email} for business {BusinessId}", 
                    existingParty.Id, request.Party.Email, request.BusinessId);
                
                partyId = existingParty.Id;
                
                // Optionally update party information if details have changed
                var hasChanges = false;
                
                if (existingParty.Name != request.Party.Name)
                {
                    existingParty.UpdateName(request.Party.Name);
                    hasChanges = true;
                }
                
                if (existingParty.Phone != request.Party.Phone || existingParty.Email != request.Party.Email)
                {
                    existingParty.UpdateContactInfo(request.Party.Phone, request.Party.Email);
                    hasChanges = true;
                }
                
                if (existingParty.TaxIdentificationNumber.Value != request.Party.TaxIdentificationNumber)
                {
                    var newTin = TIN.Create(request.Party.TaxIdentificationNumber);
                    existingParty.UpdateTaxIdentificationNumber(newTin);
                    hasChanges = true;
                }
                
                // Check if address has changed
                var currentAddress = existingParty.Address;
                var newAddress = Address.Create(
                    request.Party.Address.Street,
                    request.Party.Address.City,
                    request.Party.Address.State,
                    request.Party.Address.Country,
                    request.Party.Address.PostalCode ?? string.Empty);
                
                if (!AddressEquals(currentAddress, newAddress))
                {
                    existingParty.UpdateAddress(newAddress);
                    hasChanges = true;
                }
                
                if (hasChanges)
                {
                    existingParty.MarkAsUpdated(userId.Value);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Updated existing party {PartyId} with new details", existingParty.Id);
                }
            }
            else
            {
                _logger.LogInformation("No existing party found with email {Email}, creating new party for business {BusinessId}", 
                    request.Party.Email, request.BusinessId);
                
                var createPartyCommand = new CreatePartyCommand(
                    request.Party.Name,
                    request.Party.Phone,
                    request.Party.Email,
                    request.Party.TaxIdentificationNumber,
                    request.Party.Address,
                    request.Party.Description);

                var partyResult = await _mediator.Send(createPartyCommand, cancellationToken);

                if (!partyResult.IsSuccess || !partyResult.PartyId.HasValue)
                {
                    _logger.LogWarning("Failed to create party: {Message}", partyResult.Message);
                    return new CreateInvoiceWithPartyResult
                    {
                        Success = false,
                        Message = $"Failed to create party: {partyResult.Message}"
                    };
                }

                partyId = partyResult.PartyId.Value;
                partyCreated = true;
                _logger.LogInformation("Successfully created new party {PartyId}", partyId);
            }

            // Step 2: Create Invoice
            _logger.LogInformation("Creating invoice for party {PartyId} in business {BusinessId}", partyId, request.BusinessId);
            
            var createInvoiceCommand = new CreateInvoiceCommand
            {
                PartyId = partyId,
                IssueDate = request.IssueDate,
                InvoiceType = request.InvoiceType,
                Currency = request.Currency,
                DeliveryPeriod = request.DeliveryPeriod,
                PaymentMeans = request.PaymentMeans,
                DueDate = request.DueDate,
                Note = request.Note,
                PaymentReference = request.PaymentReference,
                PaymentTerms = request.PaymentTerms,
                InvoiceItems = request.InvoiceItems
            };

            var invoiceResult = await _mediator.Send(createInvoiceCommand, cancellationToken);

            if (!invoiceResult.IsSuccess)
            {
                _logger.LogError("Failed to create invoice: {Message}", invoiceResult.Message);
                return new CreateInvoiceWithPartyResult
                {
                    Success = false,
                    Message = $"Failed to create invoice: {invoiceResult.Message}"
                };
            }

            var successMessage = partyCreated
                ? "Invoice created successfully with new party"
                : "Invoice created successfully using existing party";

            _logger.LogInformation("Successfully created invoice with party {PartyId} for business {BusinessId}. Party was {PartyStatus}",
                partyId, request.BusinessId, partyCreated ? "created" : "reused");

            return new CreateInvoiceWithPartyResult
            {
                Success = true,
                Message = successMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice with party for business {BusinessId}", request.BusinessId);

            return new CreateInvoiceWithPartyResult
            {
                Success = false,
                Message = $"Error creating invoice with party: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Helper method to compare two Address objects for equality
    /// </summary>
    private static bool AddressEquals(Address address1, Address address2)
    {
        if (address1 == null && address2 == null) return true;
        if (address1 == null || address2 == null) return false;
        
        return address1.Street == address2.Street &&
               address1.City == address2.City &&
               address1.State == address2.State &&
               address1.Country == address2.Country &&
               address1.PostalCode == address2.PostalCode;
    }
}