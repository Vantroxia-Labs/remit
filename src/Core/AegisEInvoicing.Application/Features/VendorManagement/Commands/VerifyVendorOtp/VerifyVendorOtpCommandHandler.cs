using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.VerifyVendorOtp;

public class VerifyVendorOtpCommandHandler(
    IApplicationDbContext context,
    ILogger<VerifyVendorOtpCommandHandler> logger)
    : IRequestHandler<VerifyVendorOtpCommand, VendorPortalVerifyResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ILogger<VerifyVendorOtpCommandHandler> _logger = logger;

    public async Task<VendorPortalVerifyResult> Handle(VerifyVendorOtpCommand request, CancellationToken cancellationToken)
    {
        var bv = await _context.InvoiceBroadcastVendors
            .Include(x => x.Vendor)
            .FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);

        if (bv is null)
            return new VendorPortalVerifyResult(false, "Invalid or expired link.");

        if (!bv.IsVerificationCodeValid(request.Otp))
            return new VendorPortalVerifyResult(false, "Invalid or expired OTP. Please request a new one.");

        bv.MarkEmailVerified();
        await _context.SaveChangesAsync(cancellationToken);

        return new VendorPortalVerifyResult(
            true,
            "Email verified successfully.",
            bv.Vendor.BusinessName,
            bv.Vendor.Email,
            bv.Vendor.Phone);
    }
}
