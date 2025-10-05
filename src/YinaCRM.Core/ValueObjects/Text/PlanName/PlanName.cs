// VO: PlanName (shared)
#nullable enable
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Name of a subscription plan. Normalization: trims and collapses spaces.
/// Validation: 1–100 characters.
/// </summary>
public readonly record struct PlanName
{
    internal string Value { get; }
    private PlanName(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<PlanName> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<PlanName>.Failure(PlanNameErrors.Empty());
        var s = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "\\s+", " ");
        if (s.Length > 100)
            return Result<PlanName>.Failure(PlanNameErrors.TooLong());
        return Result<PlanName>.Success(new PlanName(s));
    }
}

public static class PlanNameErrors
{
    public static Error Empty() => Error.Create("PLANNAME_EMPTY", "Plan name is required", 400);
    public static Error TooLong() => Error.Create("PLANNAME_TOO_LONG", "Plan name must be at most 100 characters", 400);
}



