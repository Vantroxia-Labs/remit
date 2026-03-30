using AegisEInvoicing.Application.Common.Models;
using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.EnsureCerberusUserFromDb;

public record EnsureCerberusUserFromDbCommand(string Username) : IRequest<Result>;

public class EnsureCerberusUserFromDbCommandHandler : IRequestHandler<EnsureCerberusUserFromDbCommand, Result>
{
    public async Task<Result> Handle(EnsureCerberusUserFromDbCommand request, CancellationToken cancellationToken)
    {
        // SFTPGo users are created automatically on first login via external auth hook
        return await Task.FromResult(Result.Success());
    }
}
