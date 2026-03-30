using AegisEInvoicing.Application.Common.Models;
using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.AddVirtualDirectoryToUser;

public record AddVirtualDirectoryToUserCommand(string Username, string Name, string RelativePath, bool CreatePhysical) : IRequest<Result>;

public class AddVirtualDirectoryToUserCommandHandler : IRequestHandler<AddVirtualDirectoryToUserCommand, Result>
{
    public async Task<Result> Handle(AddVirtualDirectoryToUserCommand request, CancellationToken cancellationToken)
    {
        // SFTPGo manages directories automatically via permissions
        return await Task.FromResult(Result.Success());
    }
}
