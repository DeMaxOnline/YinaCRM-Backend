using System.Linq.Expressions;

namespace YinaCRM.Core.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();

    bool IsSatisfiedBy(T entity);
}
