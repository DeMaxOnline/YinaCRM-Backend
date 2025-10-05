using System;

namespace Yina.Common.Validation;

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
