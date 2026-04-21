using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Paystack.Interfaces;
using AegisEInvoicing.Paystack.Models.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.RegisterBusiness;

public class RegisterBusinessCommandHandler(
    IApplicationDbContext context,
    IPaystackService paystackService,
    IConfiguration configuration,
    ILogger<RegisterBusinessCommandHandler> logger) : IRequestHandler<RegisterBusinessCommand, RegisterBusinessResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IPaystackService _paystackService = paystackService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<RegisterBusinessCommandHandler> _logger = logger;

    public async Task<RegisterBusinessResult> Handle(RegisterBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Prevent duplicate registrations for the same email
            var existingUser = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == request.AdminEmail.ToLowerInvariant() && !u.IsDeleted, cancellationToken);

            if (existingUser)
                return new RegisterBusinessResult(false, "An account with this email already exists. Please sign in.");

            var existingPending = await _context.PendingBusinessRegistrations
                .AsNoTracking()
                .AnyAsync(p => p.AdminEmail == request.AdminEmail.ToLowerInvariant()
                    && p.Status == PendingRegistrationStatus.AwaitingPayment
                    && !p.IsDeleted, cancellationToken);

            if (existingPending)
                return new RegisterBusinessResult(false, "A pending registration already exists for this email. Please complete your payment or wait 24 hours for it to expire.");

            if (request.PlatformSubscriptionIds == null || request.PlatformSubscriptionIds.Count == 0)
                return new RegisterBusinessResult(false, "At least one subscription plan must be selected.");

            // Validate all chosen plans exist
            var planIds = request.PlatformSubscriptionIds.Distinct().ToList();
            var plans = await _context.PlatformSubscriptions
                .AsNoTracking()
                .Where(p => planIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            if (plans.Count != planIds.Count)
                return new RegisterBusinessResult(false, "One or more selected subscription plans are not valid.");

            // Total amount = sum of all selected plans
            var amountNaira = plans.Sum(p => request.BillingCycle == BillingCycle.Annual ? p.AnnualPrice : p.MonthlyPrice);
            var amountKobo = (long)(amountNaira * 100);

            // Generate a unique payment reference
            var reference = _paystackService.GenerateReference("AEGIS-REG");

            // Create pending registration record (stores all plan IDs)
            var pendingReg = PendingBusinessRegistration.Create(
                adminFirstName: request.AdminFirstName,
                adminLastName: request.AdminLastName,
                adminEmail: request.AdminEmail.ToLowerInvariant(),
                adminPhone: request.AdminPhone,
                businessName: request.BusinessName,
                tin: request.Tin,
                platformSubscriptionIds: planIds,
                billingCycle: request.BillingCycle,
                paystackReference: reference);

            await _context.PendingBusinessRegistrations.AddAsync(pendingReg, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var callbackUrl = _configuration["Paystack:CallbackUrl"]
                ?? $"{_configuration["App:BaseUrl"]}/payment/callback";

            var planNames = string.Join(", ", plans.Select(p => p.PlanName));

            // Initialize Paystack payment
            var paymentRequest = new InitializeTransactionRequest
            {
                Email = request.AdminEmail,
                Amount = amountKobo,
                Currency = "NGN",
                Reference = reference,
                CallbackUrl = callbackUrl,
                Metadata = new PaystackMetadata
                {
                    PendingRegistrationId = pendingReg.Id.ToString(),
                    PlanId = plans[0].Id.ToString(),
                    BillingCycle = request.BillingCycle.ToString(),
                    BusinessName = request.BusinessName,
                    AdminEmail = request.AdminEmail,
                    CustomFields =
                    [
                        new() { DisplayName = "Business Name", VariableName = "business_name", Value = request.BusinessName },
                        new() { DisplayName = "Plans", VariableName = "plan_name", Value = planNames },
                        new() { DisplayName = "Billing Cycle", VariableName = "billing_cycle", Value = request.BillingCycle.ToString() }
                    ]
                }
            };

            var paystackResult = await _paystackService.InitializeTransactionAsync(paymentRequest, cancellationToken);

            if (!paystackResult.Status || paystackResult.Data is null)
            {
                _logger.LogError("Paystack initialization failed for {Email}: {Message}", request.AdminEmail, paystackResult.Message);
                return new RegisterBusinessResult(false, "Payment initialization failed. Please try again.");
            }

            _logger.LogInformation("Registration initiated for {Email}. PaystackRef: {Reference}", request.AdminEmail, reference);

            return new RegisterBusinessResult(
                true,
                "Registration initiated. Please complete payment to activate your account.",
                PaymentUrl: paystackResult.Data.AuthorizationUrl,
                Reference: reference,
                PendingRegistrationId: pendingReg.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during business registration for {Email}", request.AdminEmail);
            return new RegisterBusinessResult(false, "An error occurred during registration. Please try again.");
        }
    }
}
