using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Queries.GetAllSftpUsers;

/// <summary>
/// Query to get all SFTP users from the database
/// </summary>
public class GetAllSftpUsersQuery : IRequest<IEnumerable<SftpUserDto>>
{
}

/// <summary>
/// DTO for SFTP user information
/// </summary>
public class SftpUserDto
{
    public Guid Id { get; set; }
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public SFTPUserStatus Status { get; set; }
    public string RootDirectoryPath { get; set; } = null!;
    public string WorkingDirectory { get; set; } = null!;
    public bool DirectoriesCreated { get; set; }
    public DateTimeOffset? CerberusCreatedAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}