namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Interface for managing user sessions with concurrent session limits
/// </summary>
public interface ISessionManagementService
{
    /// <summary>
    /// Gets the maximum number of concurrent sessions allowed
    /// </summary>
    int MaxConcurrentSessions { get; }

    /// <summary>
    /// Checks if a new session can be created for the user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a new session is allowed, false otherwise</returns>
    Task<bool> CanCreateSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces session limit by terminating oldest sessions if limit is exceeded
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions terminated</returns>
    Task<int> EnforceSessionLimitAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active sessions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of active sessions</returns>
    Task<int> GetActiveSessionCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates all sessions for a user except the current one
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentSessionId">Current session ID to keep active</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions terminated</returns>
    Task<int> TerminateOtherSessionsAsync(Guid userId, Guid? currentSessionId = null, CancellationToken cancellationToken = default);
}
