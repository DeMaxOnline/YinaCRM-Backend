using System.Linq.Expressions;
using YinaCRM.Core.Entities.Client;

namespace YinaCRM.Core.Specifications;

public sealed class ActiveClientSpecification : Specification<Client>
{
    public override Expression<Func<Client, bool>> ToExpression()
        => client => !client.IsDeleted;
}
