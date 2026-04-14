using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetSftpCredentials;

/// <summary>
/// Returns SFTP credentials and connection metadata for the current business.
/// </summary>
public record GetSftpCredentialsQuery : IRequest<GetSftpCredentialsResult>;
