using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Portal.API.BackgroundServices;

/// <summary>
/// Monitors vendor-submitted broadcast invoices and sends an email warning
/// when the 48-hour NRS (No Response from FIRS) window is approaching (within 1 hour).
/// </summary>
public class NrsWindowWarningService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NrsWindowWarningService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public NrsWindowWarningService(IServiceProvider serviceProvider, ILogger<NrsWindowWarningService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NRS Window Warning Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckNrsWindowAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during NRS window check");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("NRS Window Warning Service stopped");
    }

    private async Task CheckNrsWindowAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTimeOffset.UtcNow;
        // Target invoices submitted to FIRS 47–48 hours ago (approaching the 48-hour window)
        var windowStart = now.AddHours(-48);
        var windowEnd = now.AddHours(-47);

        // Find broadcast invoices where FIRS submission happened 47-48 hrs ago and are still SIGNED or TRANSMITTED
        var candidates = await context.InvoiceBroadcastVendors
            .AsNoTracking()
            .Include(bv => bv.Invoice)
            .Include(bv => bv.Vendor)
            .Include(bv => bv.InvoiceBroadcast)
                .ThenInclude(b => b.Business)
            .Where(bv =>
                bv.InvoiceId != null &&
                bv.Invoice != null &&
                bv.Invoice.SubmittedToFIRSAt.HasValue &&
                bv.Invoice.SubmittedToFIRSAt >= windowStart &&
                bv.Invoice.SubmittedToFIRSAt <= windowEnd &&
                (bv.Invoice.InvoiceStatus == InvoiceStatus.SIGNED ||
                 bv.Invoice.InvoiceStatus == InvoiceStatus.TRANSMITTED))
            .ToListAsync(cancellationToken);

        if (!candidates.Any())
            return;

        _logger.LogInformation("Found {Count} invoices approaching 48-hour NRS window", candidates.Count);

        string template;
        try
        {
            template = await File.ReadAllTextAsync(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Email", "NrsWindowWarning.html"),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load NrsWindowWarning email template");
            return;
        }

        foreach (var bv in candidates)
        {
            if (bv.Invoice is null) continue;

            try
            {
                var deadline = bv.Invoice.SubmittedToFIRSAt!.Value.AddHours(48).ToString("dd MMM yyyy HH:mm UTC");
                var submittedAt = bv.Invoice.SubmittedToFIRSAt!.Value.ToString("dd MMM yyyy HH:mm UTC");
                var irn = bv.Invoice.InvoiceCode ?? bv.Invoice.Id.ToString();

                var body = template
                    .Replace("{tenantName}", bv.InvoiceBroadcast.Business.Name)
                    .Replace("{vendorName}", bv.Vendor.BusinessName)
                    .Replace("{irn}", irn)
                    .Replace("{submittedAt}", submittedAt)
                    .Replace("{deadline}", deadline);

                await emailService.SendEmailAsync(new EmailMessage(
                    bv.InvoiceBroadcast.Business.ContactEmail,
                    $"NRS Window Warning – Invoice {irn} approaching 48-hour limit",
                    body));

                _logger.LogInformation("Sent NRS window warning for invoice {InvoiceId} to {Email}",
                    bv.InvoiceId, bv.InvoiceBroadcast.Business.ContactEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send NRS warning for invoice {InvoiceId}", bv.InvoiceId);
            }
        }
    }
}
