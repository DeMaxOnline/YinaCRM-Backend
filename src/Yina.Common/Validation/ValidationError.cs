using System;

namespace Yina.Common.Validation;

/// <summary>
/// Represents a single validation failure for a given field.
/// </summary>
public readonly record struct ValidationError(string Field, string Message, string Code = "VALIDATION_ERROR")
{
    public override string ToString() => string.IsNullOrWhiteSpace(Field)
        ? $"{Message} [{Code}]"
        : $"{Field}: {Message} [{Code}]";

    public ValidationError WithPrefix(string prefix)
        => string.IsNullOrWhiteSpace(prefix)
            ? this
            : this with { Field = string.IsNullOrWhiteSpace(Field) ? prefix : $"{prefix}.{Field}" };
}



