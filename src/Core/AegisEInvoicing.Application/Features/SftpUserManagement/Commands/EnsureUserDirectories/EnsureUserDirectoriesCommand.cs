using AegisEInvoicing.Application.Common.Models;
using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.EnsureUserDirectories;

public record EnsureUserDirectoriesCommand(string Username) : IRequest<Result>;

public class EnsureUserDirectoriesCommandHandler : IRequestHandler<EnsureUserDirectoriesCommand, Result>
{
    public async Task<Result> Handle(EnsureUserDirectoriesCommand request, CancellationToken cancellationToken)
    {
        // SFTPGo creates directories automatically via external auth hook
        return await Task.FromResult(Result.Success());
    }
}
