// Placeholder VO: JsonText (shared)
#nullable enable
using System.Text.Json;
using Yina.Common.Abstractions.Results;

namespace YinaCRM.Core.ValueObjects;

/// <summary>
/// JSON text value object.
/// Normalization: parses and re-serializes as minified JSON to produce a canonical representation.
/// Validation: must be well-formed JSON object or array (strings/numbers alone are accepted if valid JSON).
/// </summary>
public readonly record struct JsonText
{
    internal string Value { get; }
    private JsonText(string value) => Value = value;
    public override string ToString() => Value.Length <= 80 ? Value : Value[..80] + "…";

    public static Result<JsonText> TryCreate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<JsonText>.Failure(JsonTextErrors.Empty());
        try
        {
            using var doc = JsonDocument.Parse(input);
            var normalized = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            return Result<JsonText>.Success(new JsonText(normalized));
        }
        catch (JsonException)
        {
            return Result<JsonText>.Failure(JsonTextErrors.Invalid());
        }
    }
}


