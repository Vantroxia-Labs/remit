using System.Linq.Expressions;

namespace AegisEInvoicing.Domain.Specifications;

/// <summary>
/// Base specification implementation
/// </summary>
public class Specification<T> : ISpecification<T>
{
    private readonly Expression<Func<T, bool>> _expression;

    public Specification(Expression<Func<T, bool>> expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public bool IsSatisfiedBy(T entity)
    {
        return _expression.Compile().Invoke(entity);
    }

    public Expression<Func<T, bool>> ToExpression()
    {
        return _expression;
    }

    public ISpecification<T> And(ISpecification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    public ISpecification<T> Or(ISpecification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    public ISpecification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}