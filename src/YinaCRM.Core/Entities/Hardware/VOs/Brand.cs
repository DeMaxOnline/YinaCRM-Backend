// VO: Brand (optional)
#nullable enable
using System.Globalization;
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Hardware.VOs;

public readonly partial record struct Brand
{
    internal string Value { get; }
    private Brand(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<Brand> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Brand>.Failure(BrandErrors.Empty());
        var s = Regex.Replace(input.Trim(), "\\s+", " ");
        if (!Pattern().IsMatch(s) || s.Length > 80)
            return Result<Brand>.Failure(BrandErrors.Invalid());
        try
        {
            var ti = CultureInfo.InvariantCulture.TextInfo;
            s = ti.ToTitleCase(s.ToLowerInvariant());
        }
        catch { }
        return Result<Brand>.Success(new Brand(s));
    }

    [GeneratedRegex(@"^[\p{L}0-9 .&'\-]{1,80}$", RegexOptions.Compiled)]
    private static partial Regex Pattern();
}

public static class BrandErrors
{
    public static Error Empty() => Error.Create("HW_BRAND_EMPTY", "Brand is required when provided", 400);
    public static Error Invalid() => Error.Create("HW_BRAND_INVALID", "Brand must be 1–80 chars (letters, digits, spaces, . & ' -)", 400);
}


