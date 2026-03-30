using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AegisEInvoicing.Domain.Specifications;

public class OrSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public bool IsSatisfiedBy(T entity)
    {
        return _left.IsSatisfiedBy(entity) || _right.IsSatisfiedBy(entity);
    }

    public Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(leftExpression.Parameters[0], parameter);
        var rightVisitor = new ReplaceExpressionVisitor(rightExpression.Parameters[0], parameter);

        var left = leftVisitor.Visit(leftExpression.Body) ?? throw new InvalidOperationException("Failed to visit left expression");
        var right = rightVisitor.Visit(rightExpression.Body) ?? throw new InvalidOperationException("Failed to visit right expression");

        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left, right), parameter);
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