namespace Yina.Common.Abstractions.Errors;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

/// <summary>
/// Represents a rich, structured error that travels across application boundaries.
/// </summary>
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

    /// <summary>Developer-friendly error code (e.g. <c>CLIENT_NOT_FOUND</c>).</summary>
    public string Code { get; init; }

    /// <summary>Human-readable error message safe to display to end users.</summary>
    public string Message { get; init; }

    /// <summary>HTTP-style status code conveying error severity.</summary>
    public int StatusCode { get; init; }

    /// <summary>Optional field or property associated with the error.</summary>
    public string? Field { get; init; }

    /// <summary>Additional structured metadata for diagnostics.</summary>
    public ImmutableDictionary<string, string> Metadata { get; init; }

    /// <summary>A sentinel error representing success.</summary>
    public static Error None { get; } = new(string.Empty, string.Empty, 200, null, ImmutableDictionary<string, string>.Empty);

    /// <summary>Gets a value indicating whether this instance represents <see cref="None"/>.</summary>
    public bool IsNone => ReferenceEquals(this, None) || (string.IsNullOrWhiteSpace(Code) && StatusCode == 200);

    /// <summary>Creates a new error with the given metadata.</summary>
    public static Error Create(string code, string message, int statusCode = 400, string? field = null, IDictionary<string, string>? metadata = null)
    {
        var immutableMetadata = metadata is null
            ? ImmutableDictionary<string, string>.Empty
            : metadata.ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        return new Error(code, message, statusCode, field, immutableMetadata);
    }

    /// <summary>Returns a copy of the error with the specified <paramref name="field"/>.</summary>
    public Error WithField(string? field) => this with { Field = field };

    /// <summary>Returns a copy of the error with an additional metadata item.</summary>
    public Error WithDetail(string key, string value) => this with { Metadata = Metadata.SetItem(key, value) };

    /// <summary>Returns a copy of the error with merged metadata values.</summary>
    public Error MergeMetadata(IDictionary<string, string> additional)
    {
        var combined = Metadata;
        foreach (var pair in additional)
        {
            combined = combined.SetItem(pair.Key, pair.Value);
        }

        return this with { Metadata = combined };
    }

    /// <inheritdoc />
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

