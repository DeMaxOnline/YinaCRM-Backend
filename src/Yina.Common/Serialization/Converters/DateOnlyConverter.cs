using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yina.Common.Serialization.Converters;

/// <summary>JSON converter for DateOnly values.</summary>
public sealed class DateOnlyConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    /// <inheritdoc />
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s))
        {
            return default;
        }

        if (DateOnly.TryParse(s, out var date))
        {
            return date;
        }

        if (DateTime.TryParse(s, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        throw new JsonException($"Invalid DateOnly value: '{s}'");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format));
}
