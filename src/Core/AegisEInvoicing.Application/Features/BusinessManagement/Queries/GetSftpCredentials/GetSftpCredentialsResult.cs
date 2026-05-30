using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetSftpCredentials;

public record GetSftpCredentialsResult : GenericResult
{
    public SftpCredentialsDto? Credentials { get; set; }

    public new static GetSftpCredentialsResult NotFound(string message)
    {
        return new GetSftpCredentialsResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }
}

public record SftpCredentialsDto(
    string Username,
    string Host,
    int Port,
    string Status,
    string? WorkingDirectory,
    DateTimeOffset? LastSyncedAt);
