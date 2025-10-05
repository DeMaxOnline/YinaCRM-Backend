// VO: VisibilityCode (local)
#nullable enable
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Note.VOs;

/// <summary>
/// Visibility for a note: "internal" or "shared".
/// Normalization: lowercase.
/// </summary>
public readonly record struct VisibilityCode
{
    private VisibilityCode(string value) => Value = value;
    internal string Value { get; }

    public bool IsInternal => Value == "internal";
    public bool IsShared => Value == "shared";
    public override string ToString() => Value;

    public static Result<VisibilityCode> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<VisibilityCode>.Failure(Errors.Empty());
        var s = input.Trim().ToLowerInvariant();
        return s is "internal" or "shared"
            ? Result<VisibilityCode>.Success(new VisibilityCode(s))
            : Result<VisibilityCode>.Failure(Errors.Invalid());
    }

    public static class Errors
    {
        public static Error Empty() => Error.Create("NOTE_VIS_EMPTY", "Visibility is required", 400);
        public static Error Invalid() => Error.Create("NOTE_VIS_INVALID", "Visibility must be 'internal' or 'shared'", 400);
    }
}



