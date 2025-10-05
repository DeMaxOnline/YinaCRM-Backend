namespace Yina.Common.Abstractions.Errors;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public sealed record Error
{
    private Error(string code, string message, int statusCode, string? field, ImmutableDictionary<string, string> metadata)
    {
        if (string.IsNullOrWhiteSpace(code) && statusCode != 200)
        {
            throw new ArgumentException("Error code cannot be blank unless representing Error.None.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message) && statusCode != 200)
        {
            throw new ArgumentException("Error message cannot be blank unless representing Error.None.", nameof(message));
        }

        Code = code;
        Message = message;
        StatusCode = statusCode;
        Field = field;
        Metadata = metadata ?? ImmutableDictionary<string, string>.Empty;
    }

    public string Code { get; init; }

    public string Message { get; init; }

    public int StatusCode { get; init; }

    public string? Field { get; init; }

    public ImmutableDictionary<string, string> Metadata { get; init; }

    public static Error None { get; } = new(string.Empty, string.Empty, 200, null, ImmutableDictionary<string, string>.Empty);

    public bool IsNone => ReferenceEquals(this, None) || (string.IsNullOrWhiteSpace(Code) && StatusCode == 200);

    public static Error Create(string code, string message, int statusCode = 400, string? field = null, IDictionary<string, string>? metadata = null)
    {
        var immutableMetadata = metadata is null
            ? ImmutableDictionary<string, string>.Empty
            : metadata.ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        return new Error(code, message, statusCode, field, immutableMetadata);
    }

    public Error WithField(string? field) => this with { Field = field };

    public Error WithDetail(string key, string value) => this with { Metadata = Metadata.SetItem(key, value) };

    public Error MergeMetadata(IDictionary<string, string> additional)
    {
        var combined = Metadata;
        foreach (var pair in additional)
        {
            combined = combined.SetItem(pair.Key, pair.Value);
        }

        return this with { Metadata = combined };
    }

    public override string ToString()
    {
        if (IsNone)
        {
            return "None";
        }

        var detail = Metadata.Count == 0
            ? string.Empty
            : $" [{string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}]";

        var fieldSuffix = string.IsNullOrWhiteSpace(Field) ? string.Empty : $" (field: {Field})";
        return $"{Code}: {Message}{fieldSuffix}{detail}";
    }
}
