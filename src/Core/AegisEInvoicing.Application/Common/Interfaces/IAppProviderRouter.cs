namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Resolves and configures the correct <see cref="IAccessPointProviderClient"/> for a given business.
///
/// The router reads the business's <c>ActiveAppProviderCode</c> and <c>AppEnvironmentMode</c>
/// from the database, fetches the matching <c>AppProviderConfiguration</c>, decrypts the
/// appropriate credential set (sandbox or production), and returns a configured adapter.
///
/// Falls back to Interswitch when <c>ActiveAppProviderCode</c> is null or empty.
/// </summary>
public interface IAppProviderRouter
{
    /// <summary>
    /// Returns an <see cref="IAccessPointProviderClient"/> configured with the credentials
    /// that correspond to the business's active provider and environment mode.
    /// </summary>
    /// <param name="businessId">The business whose provider should be resolved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the business's active provider code has no registered adapter.
    /// </exception>
    Task<IAccessPointProviderClient> GetProviderAsync(Guid businessId, CancellationToken cancellationToken = default);
}
