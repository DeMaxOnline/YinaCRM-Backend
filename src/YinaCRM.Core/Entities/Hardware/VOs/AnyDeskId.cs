// VO: AnyDeskId (optional)
#nullable enable
using System.Linq;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Hardware.VOs;

public readonly record struct AnyDeskId
{
    internal string Value { get; }
    private AnyDeskId(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<AnyDeskId> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<AnyDeskId>.Failure(AnyDeskIdErrors.Empty());
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length != 9)
            return Result<AnyDeskId>.Failure(AnyDeskIdErrors.Invalid());
        return Result<AnyDeskId>.Success(new AnyDeskId(digits));
    }
}

public static class AnyDeskIdErrors
{
    public static Error Empty() => Error.Create("HW_ANYDESK_EMPTY", "AnyDesk id is required when provided", 400);
    public static Error Invalid() => Error.Create("HW_ANYDESK_INVALID", "AnyDesk id must be 9 digits", 400);
}


