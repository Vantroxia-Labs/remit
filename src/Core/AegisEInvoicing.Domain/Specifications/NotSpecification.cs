using System.Linq.Expressions;

namespace AegisEInvoicing.Domain.Specifications;

public class NotSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _specification;

    public NotSpecification(ISpecification<T> specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    public bool IsSatisfiedBy(T entity)
    {
        return !_specification.IsSatisfiedBy(entity);
    }

    public Expression<Func<T, bool>> ToExpression()
    {
        var expression = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var visitor = new ReplaceExpressionVisitor(expression.Parameters[0], parameter);
        var body = visitor.Visit(expression.Body) ?? throw new InvalidOperationException("Failed to visit expression body");

        return Expression.Lambda<Func<T, bool>>(Expression.Not(body), parameter);
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
