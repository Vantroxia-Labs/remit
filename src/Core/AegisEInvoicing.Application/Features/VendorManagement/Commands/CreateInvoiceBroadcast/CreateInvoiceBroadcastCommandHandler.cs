using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Entities.VendorManagement;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateInvoiceBroadcast;

public class CreateInvoiceBroadcastCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<CreateInvoiceBroadcastCommandHandler> logger) : IRequestHandler<CreateInvoiceBroadcastCommand, InvoiceBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<CreateInvoiceBroadcastCommandHandler> _logger = logger;

    public async Task<InvoiceBroadcastResult> Handle(CreateInvoiceBroadcastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new InvoiceBroadcastResult(false, "Unauthorized");

            var business = await _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == _currentUser.BusinessId.Value, cancellationToken);

            if (business is null)
                return new InvoiceBroadcastResult(false, "Business not found.");

            // Resolve target vendors
            List<Domain.Entities.VendorManagement.Vendor> vendors;
            if (request.VendorIds is { Count: > 0 })
            {
                vendors = await _context.Vendors
                    .Where(v => request.VendorIds.Contains(v.Id) && v.BusinessId == _currentUser.BusinessId.Value)
                    .ToListAsync(cancellationToken);
            }
            else if (request.VendorGroupId.HasValue)
            {
                vendors = await _context.Vendors
                    .Where(v => v.VendorGroupId == request.VendorGroupId.Value && v.BusinessId == _currentUser.BusinessId.Value)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                return new InvoiceBroadcastResult(false, "Specify at least one vendor ID or a vendor group.");
            }

            if (vendors.Count == 0)
                return new InvoiceBroadcastResult(false, "No active vendors found for the specified targets.");

            var broadcast = InvoiceBroadcast.Create(
                request.Title,
                request.InvoiceTypeCode,
                request.DueDate,
                request.RequiresApproval,
                request.Currency,
                _currentUser.BusinessId.Value,
                request.Note);

            await _context.InvoiceBroadcasts.AddAsync(broadcast, cancellationToken);

            // Create InvoiceBroadcastVendor rows
            var broadcastVendors = vendors.Select(v => InvoiceBroadcastVendor.Create(broadcast.Id, v.Id)).ToList();
            await _context.InvoiceBroadcastVendors.AddRangeAsync(broadcastVendors, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Send invite emails
            var frontendBase = request.FrontendBaseUrl
                ?? _configuration["VendorPortal:FrontendBaseUrl"]
                ?? "https://app.example.com";

            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Email", "VendorBroadcastInvite.html");
            var templateBody = await File.ReadAllTextAsync(templatePath, cancellationToken);

            foreach (var (vendor, bv) in vendors.Zip(broadcastVendors))
            {
                try
                {
                    var formLink = $"{frontendBase}/vendor-portal/{bv.Token}";
                    var noteSection = string.IsNullOrWhiteSpace(request.Note)
                        ? string.Empty
                        : $"<p style=\"font-size: 13px; color: #555; margin: 0 0 8px;\"><strong>Note:</strong> {request.Note}</p>";

                    var body = templateBody
                        .Replace("{vendorName}", vendor.BusinessName)
                        .Replace("{tenantName}", business.Name)
                        .Replace("{broadcastTitle}", broadcast.Title)
                        .Replace("{dueDate}", broadcast.DueDate.ToString("dd MMM yyyy"))
                        .Replace("{formLink}", formLink)
                        .Replace("{noteSection}", noteSection);

                    await _emailService.SendEmailAsync(new EmailMessage
                    {
                        Subject = $"Invoice Submission Request: {broadcast.Title}",
                        To = vendor.Email,
                        HtmlBody = body
                    });
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send invite email to vendor {VendorId}", vendor.Id);
                }
            }

            _logger.LogInformation("Broadcast {BroadcastId} created with {Count} vendors", broadcast.Id, vendors.Count);
            return new InvoiceBroadcastResult(true, $"Broadcast created and invites sent to {vendors.Count} vendor(s).", broadcast.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice broadcast");
            return new InvoiceBroadcastResult(false, "An error occurred while creating the broadcast.");
        }
    }
}
