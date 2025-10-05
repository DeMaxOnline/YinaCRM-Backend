// VO: ExternalHardwareId (required)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Hardware.VOs;

public readonly partial record struct ExternalHardwareId
{
    internal string Value { get; }
    private ExternalHardwareId(string value) => Value = value;
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static Result<ExternalHardwareId> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<ExternalHardwareId>.Failure(ExternalHardwareIdErrors.Empty());
        var s = input.Trim();
        if (!Pattern().IsMatch(s) || s.Length > 64)
            return Result<ExternalHardwareId>.Failure(ExternalHardwareIdErrors.Invalid());
        return Result<ExternalHardwareId>.Success(new ExternalHardwareId(s));
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$", RegexOptions.Compiled)]
    private static partial Regex Pattern();
}

public static class ExternalHardwareIdErrors
{
    public static Error Empty() => Error.Create("HW_EXTID_EMPTY", "External hardware id is required", 400);
    public static Error Invalid() => Error.Create("HW_EXTID_INVALID", "External hardware id must be 1–64 of A-Z, a-z, 0-9, ., _ or -", 400);
}


