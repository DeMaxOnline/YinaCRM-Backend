using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yina.Common.Serialization.Converters;

/// <summary>JSON converter for TimeOnly values.</summary>
public sealed class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    private const string Format = "HH:mm:ss.fffffff";

    /// <inheritdoc />
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s))
        {
            return default;
        }

        if (TimeOnly.TryParse(s, out var time))
        {
            return time;
        }

        if (DateTime.TryParse(s, out var dateTime))
        {
            return TimeOnly.FromDateTime(dateTime);
        }

        throw new JsonException($"Invalid TimeOnly value: '{s}'");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format));
}
