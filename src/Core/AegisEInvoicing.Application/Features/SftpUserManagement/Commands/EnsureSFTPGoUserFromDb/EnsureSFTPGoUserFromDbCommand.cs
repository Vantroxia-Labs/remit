using AegisEInvoicing.Application.Common.Models;
using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.EnsureSFTPGoUserFromDb;

public record EnsureSFTPGoUserFromDbCommand(string Username) : IRequest<Result>;

public class EnsureSFTPGoUserFromDbCommandHandler : IRequestHandler<EnsureSFTPGoUserFromDbCommand, Result>
{
    public async Task<Result> Handle(EnsureSFTPGoUserFromDbCommand request, CancellationToken cancellationToken)
    {
        // SFTPGo users are created automatically on first login via external auth hook
        return await Task.FromResult(Result.Success());
    }
}
