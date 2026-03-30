using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Common.Behaviours;

/// <summary>
/// Pipeline behavior for handling database transactions
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ITransactionalCommand
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IApplicationDbContext context,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            return await next();
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

            try
            {
                _logger.LogDebug(
                    "Beginning transaction for {RequestName}",
                    typeof(TRequest).Name);

                var response = await next();

                await transaction.CommitAsync(cancellationToken);

                _logger.LogDebug(
                    "Committed transaction for {RequestName}",
                    typeof(TRequest).Name);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in transaction for {RequestName}, rolling back",
                    typeof(TRequest).Name);

                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}