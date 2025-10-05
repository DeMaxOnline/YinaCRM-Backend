// VO: HardwareTypeCode (required)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Hardware.VOs;

public readonly partial record struct HardwareTypeCode
{
    internal string Value { get; }
    private HardwareTypeCode(string value) => Value = value;
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static Result<HardwareTypeCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<HardwareTypeCode>.Failure(HardwareTypeCodeErrors.Empty());
        var s = input.Trim().ToLowerInvariant();
        if (!CodePattern().IsMatch(s))
            return Result<HardwareTypeCode>.Failure(HardwareTypeCodeErrors.Invalid());
        return Result<HardwareTypeCode>.Success(new HardwareTypeCode(s));
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.Compiled)]
    private static partial Regex CodePattern();
}

public static class HardwareTypeCodeErrors
{
    public static Error Empty() => Error.Create("HW_TYPE_EMPTY", "Hardware type code is required", 400);
    public static Error Invalid() => Error.Create("HW_TYPE_INVALID", "Hardware type code must be lowercase alphanumeric with hyphens (max 64)", 400);
}


