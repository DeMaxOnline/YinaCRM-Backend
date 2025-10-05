// VO: ModuleName (shared)
#nullable enable
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Name of a subscribed module. Normalization: trims and collapses spaces.
/// Validation: 1–100 characters.
/// </summary>
public readonly record struct ModuleName
{
    internal string Value { get; }
    private ModuleName(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<ModuleName> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<ModuleName>.Failure(ModuleNameErrors.Empty());
        var s = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "\\s+", " ");
        if (s.Length > 100)
            return Result<ModuleName>.Failure(ModuleNameErrors.TooLong());
        return Result<ModuleName>.Success(new ModuleName(s));
    }
}

public static class ModuleNameErrors
{
    public static Error Empty() => Error.Create("MODULENAME_EMPTY", "Module name is required", 400);
    public static Error TooLong() => Error.Create("MODULENAME_TOO_LONG", "Module name must be at most 100 characters", 400);
}



