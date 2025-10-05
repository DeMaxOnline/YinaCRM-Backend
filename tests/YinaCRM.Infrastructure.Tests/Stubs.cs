using Microsoft.Extensions.Options;

namespace YinaCRM.Infrastructure.Tests;

internal sealed class StubOptionsMonitor<T> : IOptionsMonitor<T>
{
    public StubOptionsMonitor(T value) => CurrentValue = value;

    public T CurrentValue { get; }

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
