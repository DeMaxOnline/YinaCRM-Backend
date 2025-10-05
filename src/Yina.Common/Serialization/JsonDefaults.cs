using System.Text.Json;
using System.Text.Json.Serialization;
using Yina.Common.Serialization.Converters;

namespace Yina.Common.Serialization;

/// <summary>
/// Provides opinionated defaults for <see cref="JsonSerializerOptions"/> used across services.
/// </summary>
public static class JsonDefaults
{
    /// <summary>Creates a configured <see cref="JsonSerializerOptions"/> instance.</summary>
    public static JsonSerializerOptions Create(bool indented = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        AddConverters(options);
        return options;
    }

    /// <summary>Registers Yina-specific converters on an existing <see cref="JsonSerializerOptions"/> instance.</summary>
    public static void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new DateOnlyConverter());
        options.Converters.Add(new TimeOnlyConverter());
        options.Converters.Add(new StrongIdJsonConverterFactory());
    }
}
