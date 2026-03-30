namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Commands.AssignFIRSConfiguration;

public record AssignFIRSConfigurationResult(
    bool IsSuccess,
    string Message,
    Guid? ConfigurationId = null);