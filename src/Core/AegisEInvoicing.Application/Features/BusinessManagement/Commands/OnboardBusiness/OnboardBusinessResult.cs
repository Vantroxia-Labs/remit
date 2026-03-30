namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.OnboardBusiness;

public record OnboardBusinessResult(
bool IsSuccess,
string Message,
Guid? BusinessId = null,
string? ConnectionStatus = null);
