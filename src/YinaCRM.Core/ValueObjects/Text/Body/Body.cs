// Placeholder VO: Body (shared)
#nullable enable
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Body text value object.
/// Normalization: trims trailing/leading whitespace (internal left as-is to preserve content).
/// Validation: 1–20000 chars.
/// </summary>
public readonly record struct Body
{
    internal string Value { get; }
    private Body(string value) => Value = value;
    public override string ToString() => Value.Length <= 64 ? Value : Value[..64] + "…";

    public static Result<Body> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Body>.Failure(BodyErrors.Empty());
        var s = input.Trim();
        if (s.Length > 20000)
            return Result<Body>.Failure(BodyErrors.TooLong());
        return Result<Body>.Success(new Body(s));
    }
}


