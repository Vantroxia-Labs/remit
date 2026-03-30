using EInvoiceIntegrator.FIRSAccessPoint.Interfaces;
using EInvoiceIntegrator.FIRSAccessPoint.Models.Requests.Authentication;
using EInvoiceIntegrator.FIRSAccessPoint.Models.Requests.ReportInvoice;
using EInvoiceIntegrator.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using EInvoiceIntegrator.FIRSAccessPoint.Models.Responses.Authentication;
using EInvoiceIntegrator.FIRSAccessPoint.Models.Responses.ReportInvoice;
using Microsoft.Extensions.Logging;

namespace EInvoiceIntegrator.Application.Services;

public interface IFIRSIntegrationService
{
    Task<AuthenticationResponse> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<ReportInvoiceResponse> ProcessInvoiceAsync(ReportInvoiceRequest invoice, CancellationToken cancellationToken = default);
    Task<bool> ValidateInvoiceDataAsync(ValidateInvoiceDataRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsSystemHealthyAsync(CancellationToken cancellationToken = default);
}

public sealed class FIRSIntegrationService : IFIRSIntegrationService
{
    private readonly IFIRSHttpClient _firsClient;
    private readonly ILogger<FIRSIntegrationService> _logger;

    public FIRSIntegrationService(
        IFIRSHttpClient firsClient,
        ILogger<FIRSIntegrationService> logger)
    {
        _firsClient = firsClient ?? throw new ArgumentNullException(nameof(firsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(
        string email, 
        string password, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        _logger.LogInformation("Starting authentication process for user: {Email}", email);

        var request = new AuthenticationRequest
        {
            Email = email,
            Password = password
        };

        try
        {
            var response = await _firsClient.AuthenticateAsync(request, cancellationToken);
            
            _logger.LogInformation("Authentication successful for user: {Email}", email);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user: {Email}", email);
            throw;
        }
    }

    public async Task<ReportInvoiceResponse> ProcessInvoiceAsync(
        ReportInvoiceRequest invoice, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        _logger.LogInformation("Starting invoice processing");

        try
        {
            // Step 1: Validate invoice data
            var validationRequest = new ValidateInvoiceDataRequest
            {
                // Map from ReportInvoiceRequest to ValidateInvoiceDataRequest
                // This mapping would depend on your specific models
            };

            var isValid = await ValidateInvoiceDataAsync(validationRequest, cancellationToken);
            if (!isValid)
            {
                throw new InvalidOperationException("Invoice validation failed");
            }

            // Step 2: Report invoice to FIRS
            var response = await _firsClient.ReportInvoiceAsync(invoice, cancellationToken);

            _logger.LogInformation("Invoice processing completed successfully");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice processing failed");
            throw;
        }
    }

    public async Task<bool> ValidateInvoiceDataAsync(
        ValidateInvoiceDataRequest request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Validating invoice data");

        try
        {
            var response = await _firsClient.ValidateInvoiceDataAsync(request, cancellationToken);
            
            // Check if validation was successful based on response
            var isValid = response.Code == 200; // Adjust based on your success criteria
            
            _logger.LogInformation("Invoice data validation completed. Valid: {IsValid}", isValid);
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice data validation failed");
            return false;
        }
    }

    public async Task<bool> IsSystemHealthyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking FIRS system health");

        try
        {
            var isHealthy = await _firsClient.ValidateConnectionAsync(cancellationToken);
            
            _logger.LogInformation("FIRS system health check completed. Healthy: {IsHealthy}", isHealthy);
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FIRS system health check failed");
            return false;
        }
    }
}