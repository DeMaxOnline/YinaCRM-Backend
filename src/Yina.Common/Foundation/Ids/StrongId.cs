using System;
using System.Diagnostics.CodeAnalysis;

namespace Yina.Common.Foundation.Ids;

public readonly record struct StrongId<TTag>(Guid Value) where TTag : notnull
{
    public static StrongId<TTag> Empty => new(Guid.Empty);

    public bool IsEmpty => Value == Guid.Empty;

    public static StrongId<TTag> New() => new(Guid.NewGuid());

    public static StrongId<TTag> FromGuid(Guid value) => new(value);

    public static StrongId<TTag> FromString(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            throw new FormatException($"Value '{value}' is not a valid GUID.");
        }

        return new StrongId<TTag>(guid);
    }

    public static bool TryFromString(string? value, [MaybeNullWhen(false)] out StrongId<TTag> id)
    {
        if (Guid.TryParse(value, out var guid))
        {
            id = new StrongId<TTag>(guid);
            return true;
        }

        id = default;
        return false;
    }

    public static bool TryParse(string? value, [MaybeNullWhen(false)] out StrongId<TTag> id)
        => TryFromString(value, out id);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(StrongId<TTag> id) => id.Value;

    public static explicit operator StrongId<TTag>(Guid value) => new(value);
}
