// Placeholder VO: LocaleCode (shared)
#nullable enable
using System.Globalization;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// Locale code value object (BCP-47 / .NET culture name, e.g., en-US).
/// Normalization: trims; uses CultureInfo.Name canonical casing.
/// Validation: must map to a known CultureInfo.
/// </summary>
public readonly record struct LocaleCode
{
    internal string Value { get; }
    private LocaleCode(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<LocaleCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<LocaleCode>.Failure(LocaleCodeErrors.Empty());
        var s = input.Trim();
        try
        {
            var ci = CultureInfo.GetCultureInfo(s);
            return Result<LocaleCode>.Success(new LocaleCode(ci.Name));
        }
        catch
        {
            foreach (var ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (string.Equals(ci.Name, s, StringComparison.OrdinalIgnoreCase))
                    return Result<LocaleCode>.Success(new LocaleCode(ci.Name));
            }
            return Result<LocaleCode>.Failure(LocaleCodeErrors.Invalid());
        }
    }
}


