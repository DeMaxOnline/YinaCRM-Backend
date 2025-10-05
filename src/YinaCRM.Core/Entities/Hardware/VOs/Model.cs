// VO: Model (optional)
#nullable enable
using System.Text.RegularExpressions;
using Yina.Common.Abstractions.Errors;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.Entities.Hardware.VOs;

public readonly partial record struct Model
{
    internal string Value { get; }
    private Model(string value) => Value = value;
    public override string ToString() => Value;

    public static Result<Model> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<Model>.Failure(ModelErrors.Empty());
        var s = Regex.Replace(input.Trim(), "\\s+", " ");
        if (!Pattern().IsMatch(s) || s.Length > 80)
            return Result<Model>.Failure(ModelErrors.Invalid());
        return Result<Model>.Success(new Model(s));
    }

    [GeneratedRegex(@"^[\p{L}0-9 ._\-/]{1,80}$", RegexOptions.Compiled)]
    private static partial Regex Pattern();
}

public static class ModelErrors
{
    public static Error Empty() => Error.Create("HW_MODEL_EMPTY", "Model is required when provided", 400);
    public static Error Invalid() => Error.Create("HW_MODEL_INVALID", "Model must be 1–80 chars (letters/digits/space . _ - /)", 400);
}


