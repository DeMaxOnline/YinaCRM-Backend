using System;
using System.Collections.Concurrent;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Secrets;
using YinaCRM.Infrastructure.Support;

namespace YinaCRM.Infrastructure.Secrets;

public sealed class InMemorySecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);

    public Task<Result<string>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue(name, out var value))
        {
            return Task.FromResult(Result.Success(value));
        }

        return Task.FromResult(Result.Failure<string>(InfrastructureErrors.ValidationFailure($"Secret '{name}' not found.")));
    }

    public Task<Result> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        _secrets[name] = value;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RotateSecretAsync(string name, Func<string> nextValueFactory, CancellationToken cancellationToken = default)
    {
        _secrets[name] = nextValueFactory();
        return Task.FromResult(Result.Success());
    }
}
