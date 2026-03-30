using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceByIRN;
using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Extensions;
using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Main service that orchestrates the complete file processing workflow
/// </summary>
public class FileProcessingService(
    ISftpService sftpService,
    IXmlDeserializationService xmlDeserializationService,
    IXmlResponseService xmlResponseService,
    IMediator mediator,
    IInvoiceNotificationService notificationService,
    IOptions<ProcessingConfiguration> processingConfig,
    IOptions<NotificationConfiguration> notificationConfig,
    ILogger<FileProcessingService> logger,
    IServiceScopeFactory serviceScopeFactory) : IFileProcessingService
{
    private readonly ISftpService _sftpService = sftpService ?? throw new ArgumentNullException(nameof(sftpService));
    private readonly IXmlDeserializationService _xmlDeserializationService = xmlDeserializationService ?? throw new ArgumentNullException(nameof(xmlDeserializationService));
    private readonly IXmlResponseService _xmlResponseService = xmlResponseService ?? throw new ArgumentNullException(nameof(xmlResponseService));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IInvoiceNotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly ProcessingConfiguration _processingConfig = processingConfig.Value ?? throw new ArgumentNullException(nameof(processingConfig));
    private readonly NotificationConfiguration _notificationConfig = notificationConfig.Value ?? throw new ArgumentNullException(nameof(notificationConfig));
    private readonly ILogger<FileProcessingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    private static readonly ConcurrentDictionary<string, ProcessingLock> _processsingLocks = new();
    private static ProcessingStatistics _currentBatchStatistics = new();
    private static ServiceStatus _serviceStatus = new() { ServiceStartTime = DateTime.UtcNow };

    public async Task<ProcessingStatistics> ProcessAllPendingFilesAsync(CancellationToken cancellationToken = default)
    {
        var processingStartTime = DateTime.UtcNow;
        var allResults = new List<FileProcessingResult>();
        
        _logger.LogInformation("Starting batch processing of all pending files");
        
        try
        {
            _serviceStatus.IsRunning = true;
            _serviceStatus.LastProcessingRun = processingStartTime;
            
            // Reset current batch statistics
            _currentBatchStatistics = new ProcessingStatistics
            {
                ProcessingStartTime = processingStartTime
            };

            var enabledConnections = await _sftpService.GetEnabledConnectionsAsync(cancellationToken).ConfigureAwait(false);

            if (enabledConnections.Count == 0)
            {
                _logger.LogDebug("No enabled SFTP connections found for outgoing polling. Skipping this cycle.");
                return new ProcessingStatistics
                {
                    ProcessingStartTime = processingStartTime,
                    ProcessingEndTime = DateTime.UtcNow,
                    TotalFilesProcessed = 0,
                    SuccessfulFiles = 0,
                    ErrorFiles = 0
                };
            }

            _logger.LogInformation("Processing files from {ConnectionCount} enabled SFTP connections", enabledConnections.Count);

            if (_processingConfig.EnableParallelProcessing)
            {
                // Process connections in parallel.
                // IMPORTANT: each connection MUST run in its own DI scope so it gets its own DbContext instance.
                var connectionTasks = enabledConnections.Select(async connection =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var scopedProcessor = scope.ServiceProvider.GetRequiredService<IFileProcessingService>();

                        return await scopedProcessor.ProcessFilesFromConnectionAsync(
                            connection.ConnectionId,
                            _processingConfig.MaxFilesPerBatch,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing files from connection {ConnectionId}: {Message}",
                            connection.ConnectionId, ex.Message);
                        return new List<FileProcessingResult>();
                    }
                });

                var connectionResults = await Task.WhenAll(connectionTasks);
                allResults = connectionResults.SelectMany(r => r).ToList();
            }
            else
            {
                // Process connections sequentially
                foreach (var connection in enabledConnections)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var connectionResults = await ProcessFilesFromConnectionAsync(
                        connection.ConnectionId, 
                        _processingConfig.MaxFilesPerBatch,
                        cancellationToken);
                    
                    allResults.AddRange(connectionResults);
                }
            }

            // Calculate final statistics
            var processingEndTime = DateTime.UtcNow;
            var statistics = CalculateStatistics(allResults, processingStartTime, processingEndTime);
            
            _currentBatchStatistics = statistics;
            _serviceStatus.CurrentBatchStatistics = statistics;

            _logger.LogInformation("Batch processing completed: {TotalFiles} files processed, {SuccessfulFiles} successful, {ErrorFiles} failed, {Duration}ms",
                statistics.TotalFilesProcessed, 
                statistics.SuccessfulFiles, 
                statistics.ErrorFiles,
                statistics.TotalProcessingTime.TotalMilliseconds);

            return statistics;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File processing was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during batch processing: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _serviceStatus.IsRunning = false;
        }
    }

    public async Task<List<FileProcessingResult>> ProcessFilesFromConnectionAsync(
        string connectionId, 
        int? maxFiles = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<FileProcessingResult>();
        
        try
        {
            _logger.LogDebug("Processing files from connection: {ConnectionId}", connectionId);

            // List invoice files (XML/JSON) from SFTP connection
            var invoiceFiles = await _sftpService.ListInvoiceFilesAsync(connectionId, cancellationToken);

            if (!invoiceFiles.Any())
            {
                _logger.LogDebug("No invoice files found for connection: {ConnectionId}", connectionId);
                return results;
            }

            // Apply file limit if specified
            if (maxFiles.HasValue)
            {
                invoiceFiles = invoiceFiles.Take(maxFiles.Value).ToList();
            }

            _logger.LogInformation("Found {FileCount} invoice files to process from connection {ConnectionId}", 
                invoiceFiles.Count, connectionId);

            // Process files
            if (_processingConfig.EnableParallelProcessing)
            {
                // Each file MUST run in its own DI scope so MediatR handlers get their own DbContext.
                var semaphore = new SemaphoreSlim(Math.Max(1, _processingConfig.MaxDegreeOfParallelism));
                var processingTasks = invoiceFiles.Select(async file =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var scopedProcessor = scope.ServiceProvider.GetRequiredService<IFileProcessingService>();
                        return await scopedProcessor.ProcessSingleFileAsync(file, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var processingResults = await Task.WhenAll(processingTasks);
                results.AddRange(processingResults);
            }
            else
            {
                foreach (var file in invoiceFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = await ProcessSingleFileAsync(file, cancellationToken);
                    results.Add(result);
                }
            }

            _logger.LogInformation("Completed processing {FileCount} files from connection {ConnectionId}. Success: {SuccessCount}, Errors: {ErrorCount}",
                results.Count, connectionId, 
                results.Count(r => r.IsSuccess), 
                results.Count(r => !r.IsSuccess));

            // Disable SFTP invoice transmission for this connection after processing
            //try
            //{
            //    await _databaseSftpService.DisableSftpInvoiceTransmissionAsync(connectionId, cancellationToken);
            //    _logger.LogInformation("Disabled SFTP invoice transmission for connection: {ConnectionId}", connectionId);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Failed to disable SFTP invoice transmission for connection {ConnectionId}: {Message}", 
            //        connectionId, ex.Message);
            //}

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing files from connection {ConnectionId}: {Message}", 
                connectionId, ex.Message);
            throw;
        }
    }

    public async Task<FileProcessingResult> ProcessSingleFileAsync(
        SftpFileInfo fileInfo, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var lockId = $"{fileInfo.ConnectionId}_{fileInfo.FileName}";

        try
        {
            // Check for distributed processing lock
            if (_processingConfig.EnableDistributedProcessing)
            {
                if (!AcquireProcessingLock(lockId, fileInfo.FileName, fileInfo.ConnectionId))
                {
                    _logger.LogInformation("File {FileName} is already being processed by another instance", fileInfo.FileName);
                    return FileProcessingResult.Error(fileInfo.FileName, fileInfo.FullPath, fileInfo.ConnectionId,
                        "File is being processed by another instance", duration: stopwatch.Elapsed);
                }
            }

            _logger.LogDebug("Processing file: {FileName} from connection: {ConnectionId}", 
                fileInfo.FileName, fileInfo.ConnectionId);

            // Step 1: Download file content
            var xmlContent = await _sftpService.DownloadFileContentAsync(
                fileInfo.ConnectionId, fileInfo.FullPath, cancellationToken);

            // Step 2: Deserialize XML to CreateInvoiceRequest
            var invoiceRequest = await _xmlDeserializationService.DeserializeInvoiceRequestAsync(
                xmlContent, fileInfo.FileName, cancellationToken);

            if (invoiceRequest == null)
            {
                return await HandleProcessingError(fileInfo, "Failed to deserialize XML content", 
                    null, stopwatch.Elapsed);
            }

            // Step 3: Create invoice using existing command handler
            var createCommand = invoiceRequest.MapToCreateSFTPFIRSInvoiceCommand();
            //createCommand.InvoiceSource = InvoiceSource.SFTP;
            var createResult = await _mediator.Send(createCommand, cancellationToken);

            if (!createResult.Success)
            {
                return await HandleProcessingError(fileInfo, createResult.Message ?? "Invoice creation failed", 
                    null, stopwatch.Elapsed);
            }

            // NEW FEATURE: automatically validate, sign, and transmit the invoice after creation
            // 4: Validate invoice
            
            var validateCommand = new ValidateInvoiceCommand(
                createResult.InvoiceId ?? Guid.Empty,
                invoiceRequest.AegisBusinessId);
            var validateResult = await _mediator.Send(validateCommand, cancellationToken);

            if (!validateResult.IsSuccess)
            {
                var validationMessage = string.IsNullOrWhiteSpace(validateResult.Message)
                    ? "Invoice validation failed"
                    : validateResult.Message;

                _logger.LogWarning(
                    "Invoice {InvoiceId} created from SFTP but validation failed. Message: {Message}",
                    createResult.InvoiceId, validationMessage);

                return await HandleProcessingError(
                    fileInfo,
                    $"Validation failed: {validationMessage}",
                    null,
                    stopwatch.Elapsed);
            }

            // 5: Sign invoice
            var signCommand = new AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice.SignInvoiceCommand(
                createResult.InvoiceId ?? Guid.Empty,
                invoiceRequest.AegisBusinessId);
            var signResult = await _mediator.Send(signCommand, cancellationToken);

            if (!signResult.IsSuccess)
            {
                var signingMessage = string.IsNullOrWhiteSpace(signResult.Message)
                    ? "Invoice signing failed"
                    : signResult.Message;

                _logger.LogWarning(
                    "Invoice {InvoiceId} created and validated from SFTP but signing failed. Message: {Message}",
                    createResult.InvoiceId, signingMessage);

                return await HandleProcessingError(
                    fileInfo,
                    $"Signing failed: {signingMessage}",
                    null,
                    stopwatch.Elapsed);
            }

            // 6: Transmit invoice
            var transmitCommand = new AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitInvoice.TransmitInvoiceCommand(
                createResult.InvoiceId ?? Guid.Empty,
                invoiceRequest.AegisBusinessId);
            var transmitResult = await _mediator.Send(transmitCommand, cancellationToken);

            if (!transmitResult.IsSuccess)
            {
                var transmissionMessage = string.IsNullOrWhiteSpace(transmitResult.Message)
                    ? "Invoice transmission failed"
                    : transmitResult.Message;

                _logger.LogWarning(
                    "Invoice {InvoiceId} created, validated and signed from SFTP but transmission failed. Message: {Message}",
                    createResult.InvoiceId, transmissionMessage);

                return await HandleProcessingError(
                    fileInfo,
                    $"Transmission failed: {transmissionMessage}",
                    null,
                    stopwatch.Elapsed);
            }

            // Step 7: Send email notification (if enabled)
            if (_notificationConfig.EnableEmailNotifications && _notificationConfig.SendNotificationForEachInvoice)
            {
                try
                {
                    var notificationSent = await _notificationService.SendInvoiceSuccessNotificationAsync(
                        createResult.InvoiceId ?? Guid.Empty,
                        createResult.PartyId ?? Guid.Empty,
                        createResult.IRN ?? string.Empty,
                        fileInfo.FileName,
                        fileInfo.ConnectionId,
                        cancellationToken);

                    if (notificationSent)
                    {
                        _logger.LogInformation("Email notification sent successfully for invoice {InvoiceId}", createResult.InvoiceId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send email notification for invoice {InvoiceId}", createResult.InvoiceId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email notification for invoice {InvoiceId}: {Message}", 
                        createResult.InvoiceId, ex.Message);
                    // Don't fail the entire processing just because email notification failed
                }
            }

            // Step 8: Generate and upload ACK response
            var invoiceDetails = new InvoiceDetails
            {
                InvoiceId = createResult.InvoiceId ?? Guid.Empty,
                PartyId = createResult.PartyId ?? Guid.Empty,
                IRN = createResult.IRN ?? string.Empty,
                BusinessId = invoiceRequest.AegisBusinessId.ToString(),
                ProcessedAt = DateTime.UtcNow
            };

            var ackResponse = await _xmlResponseService.GenerateAckResponseAsync(
                invoiceDetails, fileInfo.FileName, fileInfo.ConnectionId, cancellationToken);

            await _xmlResponseService.UploadResponseAsync(ackResponse, fileInfo.ConnectionId, cancellationToken);

            // Step 8b: Upload original file (XML/JSON) and PDF to Receipts/{IRN}/ folder
            await UploadReceiptFilesToIrnFolderAsync(ackResponse, fileInfo, cancellationToken);

            // Step 9: Delete file from In-Progress (already processed)
            try
            {
                await _sftpService.DeleteFileAsync(fileInfo.ConnectionId, fileInfo.FullPath, cancellationToken);
                _logger.LogDebug("Deleted processed file from In-Progress: {FileName}", fileInfo.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {FileName} from In-Progress, continuing anyway", fileInfo.FileName);
            }

            stopwatch.Stop();

            _logger.LogInformation("Successfully processed file {FileName}: InvoiceId={InvoiceId}, PartyId={PartyId}, IRN={IRN}",
                fileInfo.FileName, createResult.InvoiceId, createResult.PartyId, createResult.IRN);

            return FileProcessingResult.Success(
                fileInfo.FileName, fileInfo.FullPath, fileInfo.ConnectionId,
                createResult.InvoiceId ?? Guid.Empty, createResult.PartyId ?? Guid.Empty, createResult.IRN ?? string.Empty,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processing of file {FileName} was cancelled", fileInfo.FileName);
            throw;
        }
        catch (Exception ex)
        {
            return await HandleProcessingError(fileInfo, ex.Message, ex, stopwatch.Elapsed);
        }
        finally
        {
            if (_processingConfig.EnableDistributedProcessing)
            {
                ReleaseProcessingLock(lockId);
            }
        }
    }

    public async Task<ServiceStatus> GetServiceStatusAsync()
    {
        try
        {
            var healthChecks = await PerformHealthChecksAsync().ConfigureAwait(false);
            var enabledConnections = await _sftpService.GetEnabledConnectionsAsync().ConfigureAwait(false);
            var activeConnections = enabledConnections.Select(c => c.ConnectionId).ToList();

            _serviceStatus.ActiveConnections = activeConnections;
            _serviceStatus.Health = DetermineOverallHealth(healthChecks);
            _serviceStatus.HealthMessages = GenerateHealthMessages(healthChecks);

            return _serviceStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service status: {Message}", ex.Message);
            
            return new ServiceStatus
            {
                Health = ServiceHealth.Critical,
                HealthMessages = new List<string> { $"Failed to get service status: {ex.Message}" }
            };
        }
    }

    public async Task<Dictionary<string, bool>> PerformHealthChecksAsync(CancellationToken cancellationToken = default)
    {
        var healthChecks = new Dictionary<string, bool>();

        try
        {
            // Test SFTP connections
            var sftpResults = await _sftpService.TestAllConnectionsAsync(cancellationToken);
            foreach (var result in sftpResults)
            {
                healthChecks[$"SFTP_{result.Key}"] = result.Value;
            }

            // Test database connectivity (simplified)
            try
            {
                // Test by creating a simple command - this would need to be implemented
                healthChecks["Database"] = true; // Placeholder
            }
            catch
            {
                healthChecks["Database"] = false;
            }

            // Test email service (simplified)
            healthChecks["EmailService"] = true; // Placeholder

            return healthChecks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health checks: {Message}", ex.Message);
            return new Dictionary<string, bool> { { "HealthCheck", false } };
        }
    }

    public async Task<int> CleanupOldFilesAsync(int olderThanDays = 30, CancellationToken cancellationToken = default)
    {
        var cleanedUpCount = 0;
        
        try
        {
            _logger.LogInformation("Starting cleanup of files older than {Days} days", olderThanDays);

            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var connections = await _sftpService.GetEnabledConnectionsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var connection in connections)
            {
                try
                {
                    var connectionCleanedCount = await CleanupConnectionDirectoriesAsync(
                        connection, cutoffDate, cancellationToken).ConfigureAwait(false);
                    cleanedUpCount += connectionCleanedCount;

                    _logger.LogDebug("Cleaned up {Count} old files from connection: {ConnectionId}",
                        connectionCleanedCount, connection.ConnectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning up files from connection {ConnectionId}: {Message}",
                        connection.ConnectionId, ex.Message);
                }
            }

            _logger.LogInformation("Cleanup completed: {FileCount} files removed", cleanedUpCount);
            return cleanedUpCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file cleanup: {Message}", ex.Message);
            throw;
        }
    }

    #region Private Methods

    private async Task UploadReceiptFilesToIrnFolderAsync(XmlResponse ackResponse, SftpFileInfo fileInfo, CancellationToken cancellationToken)
    {
        try
        {
            if (ackResponse.Invoice is null || string.IsNullOrWhiteSpace(ackResponse.Invoice.IRN))
            {
                _logger.LogWarning("Skipping Receipts/{IRN}/ upload: IRN is empty for file {FileName}", ackResponse.Invoice?.IRN, fileInfo.FileName);
                return;
            }

            var irn = ackResponse.Invoice.IRN;
            var connectionDetails = await _sftpService.GetConnectionDetailsAsync(fileInfo.ConnectionId, cancellationToken);
            if (connectionDetails == null)
            {
                _logger.LogWarning("Connection details not found for {ConnectionId}, skipping Receipts upload", fileInfo.ConnectionId);
                return;
            }

            // Build Receipts/{IRN}/ directory path
            var baseDirectory = Path.GetDirectoryName(connectionDetails.WorkingDirectory)?.Replace('\\', '/') ?? "/";
            var receiptsBaseDir = Path.Combine(baseDirectory, connectionDetails.ReceiptsDirectory).Replace('\\', '/');
            var irnFolder = Path.Combine(receiptsBaseDir, irn).Replace('\\', '/');

            _logger.LogInformation("Uploading receipt files to Receipts/{IRN}/ folder: {IrnFolder}", irn, irnFolder);

            // Ensure IRN folder exists
            await _sftpService.CreateDirectoryIfNotExistsAsync(fileInfo.ConnectionId, irnFolder, cancellationToken);

            // 1. Upload original file (keep original filename for traceability)
            var originalFilePath = Path.Combine(irnFolder, fileInfo.FileName).Replace('\\', '/');

            var originalFileContent = await _sftpService.DownloadFileContentAsync(fileInfo.ConnectionId, fileInfo.FullPath, cancellationToken);
            await _sftpService.UploadFileAsync(fileInfo.ConnectionId, originalFilePath, originalFileContent, cancellationToken);
            _logger.LogDebug("Uploaded original file to Receipts/{IRN}/ : {Path}", irn, originalFilePath);

            // 2. Upload QR code PNG to Receipts/{IRN}/{IRN}_QRCode.png
            if (!Guid.TryParse(ackResponse.Invoice.BusinessId, out var businessId))
            {
                _logger.LogWarning("Invalid BusinessId '{BusinessId}' for IRN {IRN}, skipping QR code upload", ackResponse.Invoice.BusinessId, irn);
                return;
            }

            var query = new GetInvoiceByIRNQuery
            {
                IRN = irn,
                BusinessId = businessId
            };

            var result = await _mediator.Send(query, cancellationToken);
            if (!result.Success || result.Invoice is null)
            {
                _logger.LogWarning("Failed to retrieve invoice for IRN {IRN} to get QR code: {Message}", irn, result.Message);
                return;
            }

            var qrBase64 = result.Invoice.QrCodeImage;
            if (string.IsNullOrWhiteSpace(qrBase64))
            {
                _logger.LogWarning("QR code image is empty for IRN {IRN}, skipping QR upload", irn);
                return;
            }

            byte[] pngBytes;
            try
            {
                pngBytes = Convert.FromBase64String(qrBase64);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Failed to decode QR code image for IRN {IRN} from base64", irn);
                return;
            }

            if (pngBytes.Length == 0)
            {
                _logger.LogWarning("Decoded QR code image is empty for IRN {IRN}", irn);
                return;
            }

            var qrFileName = $"{irn}_QRCode.png";
            var qrFilePath = Path.Combine(irnFolder, qrFileName).Replace('\\', '/');

            using var qrStream = new MemoryStream(pngBytes);
            await _sftpService.UploadFileAsync(fileInfo.ConnectionId, qrFilePath, qrStream, cancellationToken);
            _logger.LogInformation("Uploaded QR code PNG to Receipts/{IRN}/ : {QrPath}", irn, qrFilePath);
        }
        catch (Exception ex)
        {
            // Best-effort only: do not fail the overall processing if receipt upload fails
            _logger.LogError(ex, "Error uploading receipt files to Receipts/{IRN}/ for file {FileName}: {Message}",
                ackResponse.Invoice?.IRN ?? "UNKNOWN", fileInfo.FileName, ex.Message);
        }
    }

    private async Task<FileProcessingResult> HandleProcessingError(
        SftpFileInfo fileInfo, 
        string errorMessage, 
        Exception? exception,
        TimeSpan duration)
    {
        try
        {
            _logger.LogError(exception, "Error processing file {FileName}: {ErrorMessage}", 
                fileInfo.FileName, errorMessage);

            // Send error notification (if enabled)
            try
            {
                await _notificationService.SendInvoiceErrorNotificationAsync(
                    fileInfo.FileName,
                    fileInfo.ConnectionId,
                    errorMessage);
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, "Failed to send error notification for file {FileName}: {Message}",
                    fileInfo.FileName, notificationEx.Message);
                // Don't fail processing because of notification failure
            }

            // Generate and upload NACK response
            var errorDetails = new ErrorDetails
            {
                ErrorCode = GetErrorCodeFromException(exception) ?? "PROCESSING_ERROR",
                ErrorMessage = errorMessage,
                StackTrace = exception?.StackTrace,
                ErrorOccurredAt = DateTime.UtcNow,
                ErrorSeverity = GetErrorSeverityFromException(exception)
            };

            var nackResponse = await _xmlResponseService.GenerateNackResponseAsync(
                errorDetails, fileInfo.FileName, fileInfo.ConnectionId);

            // Upload NACK response
            try
            {
                await _xmlResponseService.UploadResponseAsync(nackResponse, fileInfo.ConnectionId);

                // Delete file from In-Progress after successful NACK upload
                try
                {
                    await _sftpService.DeleteFileAsync(fileInfo.ConnectionId, fileInfo.FullPath);
                    _logger.LogDebug("Deleted failed file from In-Progress: {FileName}", fileInfo.FileName);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to delete file {FileName} from In-Progress after NACK upload", fileInfo.FileName);
                }

                _logger.LogInformation("File {FileName} processed with error. NACK response uploaded successfully.", 
                    fileInfo.FileName);
            }
            catch (Exception uploadEx)
            {
                _logger.LogError(uploadEx, "Failed to upload NACK response for file {FileName}. File will not be moved to processed directory: {Message}", 
                    fileInfo.FileName, uploadEx.Message);
                
                // Re-throw to ensure the file processing is marked as failed and can be retried
                throw new InvalidOperationException($"NACK upload failed for file {fileInfo.FileName}: {uploadEx.Message}", uploadEx);
            }

            return FileProcessingResult.Error(fileInfo.FileName, fileInfo.FullPath, fileInfo.ConnectionId, 
                errorMessage, exception, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling processing error for file {FileName}: {Message}", 
                fileInfo.FileName, ex.Message);
            
            return FileProcessingResult.Error(fileInfo.FileName, fileInfo.FullPath, fileInfo.ConnectionId, 
                $"Original error: {errorMessage}. Error handling failed: {ex.Message}", exception, duration);
        }
    }

    private async Task GenerateAndUploadQrCodeAsync(XmlResponse ackResponse, string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            if (ackResponse.Invoice is null)
            {
                _logger.LogDebug("Skipping QR code generation for ACK {FileName}: no invoice details present", ackResponse.FileName);
                return;
            }

            if (string.IsNullOrWhiteSpace(ackResponse.Invoice.IRN))
            {
                _logger.LogDebug("Skipping QR code generation for ACK {FileName}: IRN is empty", ackResponse.FileName);
                return;
            }

            if (!Guid.TryParse(ackResponse.Invoice.BusinessId, out var businessId))
            {
                _logger.LogWarning("Skipping QR code generation for ACK {FileName}: invalid BusinessId '{BusinessId}'", ackResponse.FileName, ackResponse.Invoice.BusinessId);
                return;
            }

            // Fetch invoice details (including QR code image) via application query
            var query = new GetInvoiceByIRNQuery
            {
                IRN = ackResponse.Invoice.IRN,
                BusinessId = businessId
            };

            var result = await _mediator.Send(query, cancellationToken);
            if (!result.Success || result.Invoice is null)
            {
                _logger.LogWarning("Skipping QR code PNG generation for IRN {IRN}: unable to retrieve invoice. Message: {Message}",
                    ackResponse.Invoice.IRN, result.Message);
                return;
            }

            var qrBase64 = result.Invoice.QrCodeImage;
            if (string.IsNullOrWhiteSpace(qrBase64))
            {
                _logger.LogWarning("Skipping QR code PNG generation for IRN {IRN}: QrCodeImage is empty", ackResponse.Invoice.IRN);
                return;
            }

            byte[] pngBytes;
            try
            {
                pngBytes = Convert.FromBase64String(qrBase64);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Failed to decode QR code image for IRN {IRN} from base64", ackResponse.Invoice.IRN);
                return;
            }

            if (pngBytes.Length == 0)
            {
                _logger.LogWarning("Skipping QR code PNG upload for IRN {IRN}: decoded image is empty", ackResponse.Invoice.IRN);
                return;
            }

            // Build PNG file name based on the ACK file name
            var ackBaseName = Path.GetFileNameWithoutExtension(ackResponse.FileName);
            var qrFileName = $"{ackBaseName}_QRCode.png";
            var remoteFilePath = Path.Combine(ackResponse.TargetDirectory, qrFileName).Replace('\\', '/');

            using var ms = new MemoryStream(pngBytes);
            await _sftpService.UploadFileAsync(connectionId, remoteFilePath, ms, cancellationToken);

            _logger.LogInformation("QR code PNG generated and uploaded for IRN {IRN} as {FileName} to {Path}",
                ackResponse.Invoice.IRN, qrFileName, remoteFilePath);
        }
        catch (Exception ex)
        {
            // Best-effort only: do not fail the overall processing if QR generation/upload fails
            _logger.LogError(ex, "Error generating or uploading QR code PNG for ACK {FileName}: {Message}",
                ackResponse.FileName, ex.Message);
        }
    }

    private ProcessingStatistics CalculateStatistics(List<FileProcessingResult> results, DateTime startTime, DateTime endTime)
    {
        var statistics = new ProcessingStatistics
        {
            ProcessingStartTime = startTime,
            ProcessingEndTime = endTime,
            TotalProcessingTime = endTime - startTime,
            TotalFilesProcessed = results.Count,
            SuccessfulFiles = results.Count(r => r.IsSuccess),
            ErrorFiles = results.Count(r => !r.IsSuccess),
            SkippedFiles = results.Count(r => r.Action == ProcessingAction.Skipped),
            ErrorSummary = results.Where(r => !r.IsSuccess && !string.IsNullOrEmpty(r.ErrorMessage))
                                 .Select(r => r.ErrorMessage!)
                                 .Distinct()
                                 .ToList(),
            ConnectionStats = results.GroupBy(r => r.ConnectionId)
                                   .ToDictionary(g => g.Key, g => g.Count())
        };

        return statistics;
    }

    private bool AcquireProcessingLock(string lockId, string fileName, string connectionId)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_processingConfig.DistributedLockTimeoutMinutes);
        var processingLock = new ProcessingLock
        {
            LockId = lockId,
            FileName = fileName,
            ConnectionId = connectionId,
            AcquiredAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        return _processsingLocks.TryAdd(lockId, processingLock);
    }

    private void ReleaseProcessingLock(string lockId)
    {
        _processsingLocks.TryRemove(lockId, out _);
    }

    private string? GetErrorCodeFromException(Exception? exception)
    {
        if (exception == null) return null;

        return exception switch
        {
            ArgumentNullException => "VALIDATION_ERROR",
            ArgumentOutOfRangeException => "VALIDATION_ERROR",
            ArgumentException => "VALIDATION_ERROR",
            InvalidOperationException => "BUSINESS_RULE_VIOLATION",
            UnauthorizedAccessException => "AUTHORIZATION_ERROR",
            TimeoutException => "TIMEOUT_ERROR",
            FileNotFoundException => "FILE_NOT_FOUND",
            DirectoryNotFoundException => "DIRECTORY_NOT_FOUND",
            _ when exception.GetType().Name.Contains("BadRequest") => "VALIDATION_ERROR",
            _ when exception.GetType().Name.Contains("NotFound") => "RESOURCE_NOT_FOUND",
            _ when exception.GetType().Name.Contains("Unauthorized") => "AUTHORIZATION_ERROR",
            _ when exception.GetType().Name.Contains("Forbidden") => "AUTHORIZATION_ERROR",
            _ => "PROCESSING_ERROR"
        };
    }

    private string GetErrorSeverityFromException(Exception? exception)
    {
        if (exception == null) return "Medium";

        return exception switch
        {
            ArgumentNullException => "Medium",
            ArgumentOutOfRangeException => "Medium",
            ArgumentException => "Medium",
            InvalidOperationException => "High",
            UnauthorizedAccessException => "High",
            TimeoutException => "Medium",
            FileNotFoundException => "Medium",
            DirectoryNotFoundException => "Medium",
            _ when exception.GetType().Name.Contains("BadRequest") => "Medium",
            _ when exception.GetType().Name.Contains("NotFound") => "Medium",
            _ when exception.GetType().Name.Contains("Unauthorized") => "High",
            _ when exception.GetType().Name.Contains("Forbidden") => "High",
            _ => "High"
        };
    }

    private ServiceHealth DetermineOverallHealth(Dictionary<string, bool> healthChecks)
    {
        if (!healthChecks.Any()) return ServiceHealth.Unknown;

        var allHealthy = healthChecks.All(h => h.Value);
        var anyHealthy = healthChecks.Any(h => h.Value);

        return allHealthy ? ServiceHealth.Healthy :
               anyHealthy ? ServiceHealth.Warning : ServiceHealth.Critical;
    }

    private List<string> GenerateHealthMessages(Dictionary<string, bool> healthChecks)
    {
        var messages = new List<string>();

        foreach (var check in healthChecks)
        {
            if (!check.Value)
            {
                messages.Add($"{check.Key} is unhealthy");
            }
        }

        if (!messages.Any())
        {
            messages.Add("All systems healthy");
        }

        return messages;
    }

    private async Task<int> CleanupConnectionDirectoriesAsync(
        SftpConnectionDetails connection,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        var cleanedCount = 0;

        // Get directories to clean
        var directoriesToClean = new[]
        {
            connection.ReceiptsDirectory,
            connection.RejectedDirectory
        };

        foreach (var directory in directoriesToClean)
        {
            if (string.IsNullOrWhiteSpace(directory))
                continue;

            try
            {
                // Resolve the full directory path
                var fullPath = Path.IsPathRooted(directory)
                    ? directory
                    : Path.Combine(connection.WorkingDirectory, directory).Replace('\\', '/');

                // List files in the directory
                var files = await _sftpService.ListInvoiceFilesAsync(connection.ConnectionId, cancellationToken)
                    .ConfigureAwait(false);

                // Filter and delete old files
                foreach (var file in files.Where(f => f.LastModified < cutoffDate))
                {
                    try
                    {
                        await _sftpService.DeleteFileAsync(connection.ConnectionId, file.FullPath, cancellationToken)
                            .ConfigureAwait(false);
                        cleanedCount++;

                        _logger.LogDebug("Deleted old file {FileName} (modified: {LastModified})",
                            file.FileName, file.LastModified);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old file {FileName}: {Message}",
                            file.FileName, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup directory {Directory} on connection {ConnectionId}: {Message}",
                    directory, connection.ConnectionId, ex.Message);
            }
        }

        return cleanedCount;
    }

    #endregion
}