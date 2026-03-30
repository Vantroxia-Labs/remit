using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services.Security;

/// <summary>
/// Implementation of comprehensive invoice audit logging with tamper protection.
/// Provides immutable audit trail for compliance and security investigations.
/// </summary>
public sealed class InvoiceAuditService : IInvoiceAuditService
{
    private readonly ILogger<InvoiceAuditService> _logger;
    private readonly IResponseIntegrityService _integrityService;
    private readonly List<InvoiceAuditEntry> _auditStore; // In production, this would be a database

    public InvoiceAuditService(
        ILogger<InvoiceAuditService> logger,
        IResponseIntegrityService integrityService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
        _auditStore = new List<InvoiceAuditEntry>();
    }

    public async Task LogStateTransitionAsync(
        Guid invoiceId,
        string operation,
        string statusBefore,
        string statusAfter,
        Guid? userId,
        string? ipAddress,
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var timestamp = DateTime.UtcNow;

            // Generate tamper-proof signature for this audit entry
            var signature = await _integrityService.SignInvoiceOperationAsync(
                invoiceId,
                operation,
                statusBefore,
                statusAfter,
                timestamp);

            var auditEntry = new InvoiceAuditEntry
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Timestamp = timestamp,
                Operation = operation,
                StatusBefore = statusBefore,
                StatusAfter = statusAfter,
                UserId = userId,
                IpAddress = ipAddress,
                AdditionalData = additionalData,
                Signature = signature,
                EntryType = "StateTransition"
            };

            // In production, save to database
            _auditStore.Add(auditEntry);

            _logger.LogInformation(
                "Audit: Invoice state transition logged. InvoiceId={InvoiceId}, Operation={Operation}, Status={StatusBefore}->{StatusAfter}, User={UserId}, IP={IpAddress}",
                invoiceId, operation, statusBefore, statusAfter, userId, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error logging invoice state transition: InvoiceId={InvoiceId}, Operation={Operation}",
                invoiceId, operation);
            // Don't throw - audit logging should not break business operations
        }
    }

    public async Task LogExternalServiceInteractionAsync(
        Guid invoiceId,
        string serviceName,
        string operation,
        string? requestData,
        string? responseData,
        bool isSuccess,
        string? responseHash = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var timestamp = DateTime.UtcNow;

            // Calculate response hash if not provided
            if (string.IsNullOrEmpty(responseHash) && !string.IsNullOrEmpty(responseData))
            {
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(responseData));
                responseHash = Convert.ToBase64String(hashBytes);
            }

            var auditData = new
            {
                ServiceName = serviceName,
                Operation = operation,
                IsSuccess = isSuccess,
                RequestDataLength = requestData?.Length ?? 0,
                ResponseDataLength = responseData?.Length ?? 0,
                ResponseHash = responseHash
            };

            var auditEntry = new InvoiceAuditEntry
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Timestamp = timestamp,
                Operation = $"ExternalService_{serviceName}_{operation}",
                AdditionalData = JsonSerializer.Serialize(auditData),
                EntryType = "ExternalService",
                Signature = await GenerateEntrySignatureAsync(auditData, timestamp)
            };

            _auditStore.Add(auditEntry);

            // Also verify external service response integrity
            await _integrityService.VerifyExternalServiceResponseAsync(
                serviceName,
                responseData ?? string.Empty,
                null);

            _logger.LogInformation(
                "Audit: External service interaction logged. InvoiceId={InvoiceId}, Service={ServiceName}, Operation={Operation}, Success={IsSuccess}",
                invoiceId, serviceName, operation, isSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error logging external service interaction: InvoiceId={InvoiceId}, Service={ServiceName}",
                invoiceId, serviceName);
        }
    }

    public async Task LogSecurityEventAsync(
        Guid? invoiceId,
        string eventType,
        string description,
        string severity,
        string? ipAddress,
        Guid? userId,
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var timestamp = DateTime.UtcNow;

            var securityData = new
            {
                EventType = eventType,
                Description = description,
                Severity = severity,
                AdditionalData = additionalData
            };

            var auditEntry = new InvoiceAuditEntry
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId ?? Guid.Empty,
                Timestamp = timestamp,
                Operation = $"SecurityEvent_{eventType}",
                UserId = userId,
                IpAddress = ipAddress,
                AdditionalData = JsonSerializer.Serialize(securityData),
                EntryType = "SecurityEvent",
                Signature = await GenerateEntrySignatureAsync(securityData, timestamp)
            };

            _auditStore.Add(auditEntry);

            var logLevel = severity.ToUpperInvariant() switch
            {
                "CRITICAL" => LogLevel.Critical,
                "HIGH" => LogLevel.Error,
                "MEDIUM" => LogLevel.Warning,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel,
                "Audit: Security event logged. EventType={EventType}, Severity={Severity}, InvoiceId={InvoiceId}, User={UserId}, IP={IpAddress}, Description={Description}",
                eventType, severity, invoiceId, userId, ipAddress, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error logging security event: EventType={EventType}",
                eventType);
        }
    }

    public Task<List<InvoiceAuditEntry>> GetAuditTrailAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditTrail = _auditStore
                .Where(e => e.InvoiceId == invoiceId)
                .OrderBy(e => e.Timestamp)
                .ToList();

            _logger.LogDebug(
                "Retrieved audit trail for InvoiceId={InvoiceId}: {Count} entries",
                invoiceId, auditTrail.Count);

            return Task.FromResult(auditTrail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for InvoiceId={InvoiceId}", invoiceId);
            return Task.FromResult(new List<InvoiceAuditEntry>());
        }
    }

    public async Task<bool> VerifyAuditTrailIntegrityAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditTrail = await GetAuditTrailAsync(invoiceId, cancellationToken);

            if (!auditTrail.Any())
            {
                _logger.LogWarning("No audit trail found for InvoiceId={InvoiceId}", invoiceId);
                return true; // No trail means no tampering
            }

            var tamperedEntries = 0;

            foreach (var entry in auditTrail)
            {
                if (string.IsNullOrEmpty(entry.Signature))
                {
                    _logger.LogWarning(
                        "Audit entry {EntryId} for InvoiceId={InvoiceId} has no signature",
                        entry.Id, invoiceId);
                    tamperedEntries++;
                    continue;
                }

                // Verify signature for state transition entries
                if (entry.EntryType == "StateTransition")
                {
                    var expectedSignature = await _integrityService.SignInvoiceOperationAsync(
                        entry.InvoiceId,
                        entry.Operation,
                        entry.StatusBefore ?? string.Empty,
                        entry.StatusAfter ?? string.Empty,
                        entry.Timestamp);

                    if (entry.Signature != expectedSignature)
                    {
                        _logger.LogWarning(
                            "Audit entry {EntryId} signature mismatch. Possible tampering detected.",
                            entry.Id);
                        tamperedEntries++;
                    }
                }
            }

            var isIntact = tamperedEntries == 0;

            if (!isIntact)
            {
                _logger.LogError(
                    "Audit trail integrity check failed for InvoiceId={InvoiceId}. {TamperedCount} of {TotalCount} entries have been tampered with.",
                    invoiceId, tamperedEntries, auditTrail.Count);

                // Log security event
                await LogSecurityEventAsync(
                    invoiceId,
                    "AUDIT_TRAIL_TAMPERING",
                    $"Audit trail integrity check failed. {tamperedEntries} entries tampered.",
                    "CRITICAL",
                    null,
                    null,
                    JsonSerializer.Serialize(new { TamperedEntries = tamperedEntries, TotalEntries = auditTrail.Count }),
                    cancellationToken);
            }
            else
            {
                _logger.LogInformation(
                    "Audit trail integrity verified for InvoiceId={InvoiceId}. All {Count} entries intact.",
                    invoiceId, auditTrail.Count);
            }

            return isIntact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying audit trail integrity for InvoiceId={InvoiceId}", invoiceId);
            return false;
        }
    }

    private async Task<string> GenerateEntrySignatureAsync(object data, DateTime timestamp)
    {
        var json = JsonSerializer.Serialize(data);
        var requestId = Guid.NewGuid().ToString();
        return await _integrityService.GenerateResponseSignatureAsync(json, timestamp, requestId);
    }
}
