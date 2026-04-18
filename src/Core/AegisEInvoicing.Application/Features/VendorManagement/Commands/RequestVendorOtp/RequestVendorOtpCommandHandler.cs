using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Application.Interfaces;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.RequestVendorOtp;

public class RequestVendorOtpCommandHandler(
    IApplicationDbContext context,
    IEmailService emailService,
    ILogger<RequestVendorOtpCommandHandler> logger)
    : IRequestHandler<RequestVendorOtpCommand, VendorPortalCommandResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<RequestVendorOtpCommandHandler> _logger = logger;

    public async Task<VendorPortalCommandResult> Handle(RequestVendorOtpCommand request, CancellationToken cancellationToken)
    {
        var bv = await _context.InvoiceBroadcastVendors
            .Include(x => x.InvoiceBroadcast)
            .Include(x => x.Vendor)
            .FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);

        if (bv is null)
            return new VendorPortalCommandResult(false, "Invalid or expired link.");

        if (!bv.InvoiceBroadcast.IsActive || bv.InvoiceBroadcast.DueDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return new VendorPortalCommandResult(false, "This broadcast is no longer active.");

        var otp = Random.Shared.Next(100000, 999999).ToString();
        bv.SetVerificationCode(otp, DateTimeOffset.UtcNow.AddMinutes(10));

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var template = await File.ReadAllTextAsync(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Email", "VendorOtpEmail.html"),
                cancellationToken);

            var body = template
                .Replace("{vendorName}", bv.Vendor.BusinessName)
                .Replace("{broadcastTitle}", bv.InvoiceBroadcast.Title)
                .Replace("{otp}", otp);

            await _emailService.SendEmailAsync(new EmailMessage(
                bv.Vendor.Email,
                "Your Invoice Submission OTP",
                body));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to vendor {VendorId}", bv.VendorId);
            return new VendorPortalCommandResult(false, "OTP generated but email delivery failed. Please try again.");
        }

        return new VendorPortalCommandResult(true, "OTP sent to your registered email address.");
    }
}
