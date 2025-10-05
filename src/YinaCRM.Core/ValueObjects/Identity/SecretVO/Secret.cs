// Placeholder VO: Secret (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects.Identity.SecretVO;

/// <summary>
/// Opaque secret value.
/// Normalization: none; content is preserved as-is.
/// Validation: non-null, not empty or whitespace-only.
/// ToString is masked to avoid leaking secrets.
/// </summary>
public readonly record struct Secret
{
    internal string Value { get; }
    private Secret(string value) => Value = value;
    public override string ToString() => new string('*', Math.Min(Value.Length, 8));

    public static Result<Secret> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Secret>.Failure(SecretErrors.Empty());
        return Result<Secret>.Success(new Secret(input));
    }
}


