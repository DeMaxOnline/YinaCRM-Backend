// VO: HardwareDetailTypeCode (required)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Hardware.VOs;

public readonly partial record struct HardwareDetailTypeCode
{
    internal string Value { get; }
    private HardwareDetailTypeCode(string value) => Value = value;
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static Result<HardwareDetailTypeCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<HardwareDetailTypeCode>.Failure(HardwareDetailTypeCodeErrors.Empty());
        var s = input.Trim().ToLowerInvariant();
        if (!CodePattern().IsMatch(s))
            return Result<HardwareDetailTypeCode>.Failure(HardwareDetailTypeCodeErrors.Invalid());
        return Result<HardwareDetailTypeCode>.Success(new HardwareDetailTypeCode(s));
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.Compiled)]
    private static partial Regex CodePattern();
}

public static class HardwareDetailTypeCodeErrors
{
    public static Error Empty() => Error.Create("HW_DETAILTYPE_EMPTY", "Hardware detail type code is required", 400);
    public static Error Invalid() => Error.Create("HW_DETAILTYPE_INVALID", "Hardware detail type code must be lowercase alphanumeric with hyphens (max 64)", 400);
}


