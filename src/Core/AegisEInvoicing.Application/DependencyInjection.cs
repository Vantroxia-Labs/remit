using AegisEInvoicing.Application.Common.Behaviours;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Services;
using AegisEInvoicing.Application.Services.Authentication;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AegisEInvoicing.Application;

/// <summary>
/// Dependency injection configuration for Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(m =>
        {
            m.AddMaps(Assembly.GetExecutingAssembly());
        });

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Application Services
        services.AddScoped<IApiKeyAuthenticationService, ApiKeyAuthenticationService>();
        services.AddScoped<IInvoiceReferenceValidator, InvoiceReferenceValidator>();

        // Register MediatR command handlers explicitly for orchestrator
        services.AddScoped<Features.InvoiceManagement.Commands.CreateFIRSInvoice.CreateFIRSInvoiceCommandHandler>();
        services.AddScoped<Features.InvoiceManagement.Commands.ValidateInvoice.ValidateInvoiceCommandHandler>();
        services.AddScoped<Features.InvoiceManagement.Commands.SignInvoice.SignInvoiceCommandHandler>();
        services.AddScoped<Features.InvoiceManagement.Commands.TransmitInvoice.TransmitInvoiceCommandHandler>();

        // Invoice Submission Orchestrator - HIGH PERFORMANCE!
        services.AddScoped<InvoiceSubmissionOrchestrator>();

        // FlowRule Services
        services.AddScoped<IFlowRuleValidationService, FlowRuleValidationService>();
        services.AddScoped<IFlowRuleMatchingService, FlowRuleMatchingService>();

        return services;
    }
}