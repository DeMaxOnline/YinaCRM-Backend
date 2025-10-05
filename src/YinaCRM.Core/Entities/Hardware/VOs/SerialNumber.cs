// VO: SerialNumber (optional)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Hardware.VOs;

public readonly partial record struct SerialNumber
{
    internal string Value { get; }
    private SerialNumber(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<SerialNumber> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<SerialNumber>.Failure(SerialNumberErrors.Empty());
        var s = input.Trim().ToUpperInvariant();
        if (!Pattern().IsMatch(s) || s.Length > 64)
            return Result<SerialNumber>.Failure(SerialNumberErrors.Invalid());
        return Result<SerialNumber>.Success(new SerialNumber(s));
    }

    [GeneratedRegex("^[A-Z0-9-]{1,64}$", RegexOptions.Compiled)]
    private static partial Regex Pattern();
}

public static class SerialNumberErrors
{
    public static Error Empty() => Error.Create("HW_SN_EMPTY", "Serial number is required when provided", 400);
    public static Error Invalid() => Error.Create("HW_SN_INVALID", "Serial number must be 1–64 characters of A–Z, 0–9 or '-'", 400);
}


