using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ChangeSftpPassword;

/// <summary>
/// Command to change an SFTP user password in both Cerberus and database
/// </summary>
public class ChangeSftpPasswordCommand : IRequest<ChangeSftpPasswordResult>, ITransactionalCommand
{
    public string Username { get; set; } = null!;
    public string? OldPassword { get; set; } = null;
    public string NewPassword { get; set; } = null!;

    public ChangeSftpPasswordCommand(string username, string oldpassword, string newPassword)
    {
        Username = username;
        OldPassword = oldpassword;
        NewPassword = newPassword;
    }
}

/// <summary>
/// Result of SFTP user password change operation
/// </summary>
public class ChangeSftpPasswordResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = null!;

    public ChangeSftpPasswordResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }
}