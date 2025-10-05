using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Auth.Auth0;

public interface IAuth0ManagementTokenSource
{
    Task<Result<string>> GetTokenAsync(CancellationToken cancellationToken);
}
