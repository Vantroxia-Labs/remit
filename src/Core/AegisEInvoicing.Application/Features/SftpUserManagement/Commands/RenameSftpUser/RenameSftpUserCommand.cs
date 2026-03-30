using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.RenameSftpUser;

/// <summary>
/// Command to rename an SFTP user in both Cerberus and database
/// </summary>
public class RenameSftpUserCommand : IRequest<RenameSftpUserResult>, ITransactionalCommand
{
    public string Username { get; set; } = null!;
    public string NewUsername { get; set; } = null!;

    public RenameSftpUserCommand(string username, string newUsername)
    {
        Username = username;
        NewUsername = newUsername;
    }
}

/// <summary>
/// Result of SFTP user rename operation
/// </summary>
public class RenameSftpUserResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = null!;

    public RenameSftpUserResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }
}