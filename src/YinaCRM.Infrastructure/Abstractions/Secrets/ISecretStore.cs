using Yina.Common.Abstractions.Results;

namespace YinaCRM.Infrastructure.Abstractions.Secrets;

public interface ISecretStore
{
    Task<Result<string>> GetSecretAsync(string name, CancellationToken cancellationToken = default);

    Task<Result> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default);

    Task<Result> RotateSecretAsync(string name, Func<string> nextValueFactory, CancellationToken cancellationToken = default);
}
