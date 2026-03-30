using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.DeleteSftpUser;

/// <summary>
/// Command to delete an SFTP user from both Cerberus and database
/// </summary>
public class DeleteSftpUserCommand : IRequest<DeleteSftpUserResult>, ITransactionalCommand
{
    public string Username { get; set; } = null!;

    public DeleteSftpUserCommand(string username)
    {
        Username = username;
    }
}

/// <summary>
/// Result of SFTP user deletion operation
/// </summary>
public class DeleteSftpUserResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = null!;

    public DeleteSftpUserResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }
}