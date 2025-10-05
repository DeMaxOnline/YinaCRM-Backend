using System.Linq.Expressions;

namespace YinaCRM.Core.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public virtual bool IsSatisfiedBy(T entity) => ToExpression().Compile().Invoke(entity);

    public Specification<T> And(ISpecification<T> other) => new AndSpecification<T>(this, other);

    public Specification<T> Or(ISpecification<T> other) => new OrSpecification<T>(this, other);

    public Specification<T> Not() => new NotSpecification<T>(this);

    private sealed class AndSpecification<TSpec> : Specification<TSpec>
    {
        private readonly ISpecification<TSpec> _left;
        private readonly ISpecification<TSpec> _right;

        public AndSpecification(ISpecification<TSpec> left, ISpecification<TSpec> right)
        {
            _left = left;
            _right = right;
        }

        public override Expression<Func<TSpec, bool>> ToExpression()
        {
            var left = _left.ToExpression();
            var right = _right.ToExpression();
            var parameter = Expression.Parameter(typeof(TSpec));
            var body = Expression.AndAlso(
                Expression.Invoke(left, parameter),
                Expression.Invoke(right, parameter));
            return Expression.Lambda<Func<TSpec, bool>>(body, parameter);
        }
    }

    private sealed class OrSpecification<TSpec> : Specification<TSpec>
    {
        private readonly ISpecification<TSpec> _left;
        private readonly ISpecification<TSpec> _right;

        public OrSpecification(ISpecification<TSpec> left, ISpecification<TSpec> right)
        {
            _left = left;
            _right = right;
        }

        public override Expression<Func<TSpec, bool>> ToExpression()
        {
            var left = _left.ToExpression();
            var right = _right.ToExpression();
            var parameter = Expression.Parameter(typeof(TSpec));
            var body = Expression.OrElse(
                Expression.Invoke(left, parameter),
                Expression.Invoke(right, parameter));
            return Expression.Lambda<Func<TSpec, bool>>(body, parameter);
        }
    }

    private sealed class NotSpecification<TSpec> : Specification<TSpec>
    {
        private readonly ISpecification<TSpec> _inner;

        public NotSpecification(ISpecification<TSpec> inner)
        {
            _inner = inner;
        }

        public override Expression<Func<TSpec, bool>> ToExpression()
        {
            var expression = _inner.ToExpression();
            var parameter = expression.Parameters[0];
            var body = Expression.Not(expression.Body);
            return Expression.Lambda<Func<TSpec, bool>>(body, parameter);
        }
    }
}
