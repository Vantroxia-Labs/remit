using AegisEInvoicing.Application.Features.BusinessOnboarding.DTOs;
using AegisEInvoicing.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessOnboarding.Queries.GetDeploymentOptions;

public class GetDeploymentOptionsQueryHandler : IRequestHandler<GetDeploymentOptionsQuery, IEnumerable<DeploymentOptionDto>>
{
    private readonly ILogger<GetDeploymentOptionsQueryHandler> _logger;

    public GetDeploymentOptionsQueryHandler(ILogger<GetDeploymentOptionsQueryHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IEnumerable<DeploymentOptionDto>> Handle(GetDeploymentOptionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var options = new List<DeploymentOptionDto>
            {
                new()
                {
                    Type = BusinessDeploymentType.SaaS,
                    Name = "Software as a Service (SaaS)",
                    Description = "Cloud-hosted solution managed by KMPG. No infrastructure setup required.",
                    Requirements = new List<string>
                    {
                        "Valid business registration",
                        "Tax Identification Number (TIN)",
                        "Contact person details",
                        "FIRS credentials (optional - can be configured later)"
                    },
                    EstimatedSetupTime = "1-3 business days after approval",
                    MonthlyCost = "Contact KMPG for pricing"
                },
                new()
                {
                    Type = BusinessDeploymentType.OnPremise,
                    Name = "On-Premise Deployment",
                    Description = "Self-hosted solution deployed on your infrastructure.",
                    Requirements = new List<string>
                    {
                        "Valid business registration",
                        "Tax Identification Number (TIN)", 
                        "Contact person details",
                        "FIRS API credentials (required)",
                        "Infrastructure specifications",
                        "Domain whitelist for security",
                        "Technical contact for setup"
                    },
                    EstimatedSetupTime = "2-4 weeks after approval",
                    MonthlyCost = "Contact KMPG for pricing"
                }
            };

            return Task.FromResult<IEnumerable<DeploymentOptionDto>>(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deployment options");
            throw;
        }
    }
}