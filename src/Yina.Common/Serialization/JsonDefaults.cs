using System.Text.Json;
using System.Text.Json.Serialization;
using Yina.Common.Serialization.Converters;

namespace Yina.Common.Serialization;

public static class JsonDefaults
{
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

    public static void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new DateOnlyConverter());
        options.Converters.Add(new TimeOnlyConverter());
        options.Converters.Add(new StrongIdJsonConverterFactory());
    }
}
