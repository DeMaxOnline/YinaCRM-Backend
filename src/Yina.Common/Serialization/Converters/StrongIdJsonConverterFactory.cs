using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yina.Common.Foundation.Ids;

namespace Yina.Common.Serialization.Converters;

/// <summary>Factory for <see cref="JsonConverter"/> instances handling <see cref="StrongId{T}"/>.</summary>
public sealed class StrongIdJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericDefinition = typeToConvert.GetGenericTypeDefinition();
        if (genericDefinition == typeof(StrongId<>))
        {
            return true;
        }

        if (genericDefinition == typeof(Nullable<>))
        {
            var inner = typeToConvert.GetGenericArguments()[0];
            return CanConvert(inner);
        }

        return false;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var genericDefinition = typeToConvert.GetGenericTypeDefinition();
        if (genericDefinition == typeof(StrongId<>))
        {
            var tagType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(StrongIdConverter<>).MakeGenericType(tagType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        if (genericDefinition == typeof(Nullable<>))
        {
            var strongIdType = typeToConvert.GetGenericArguments()[0];
            if (!strongIdType.IsGenericType || strongIdType.GetGenericTypeDefinition() != typeof(StrongId<>))
            {
                throw new NotSupportedException($"Type '{typeToConvert}' is not supported by {nameof(StrongIdJsonConverterFactory)}.");
            }

            var tagType = strongIdType.GetGenericArguments()[0];
            var converterType = typeof(NullableStrongIdConverter<>).MakeGenericType(tagType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        throw new NotSupportedException($"Type '{typeToConvert}' is not supported by {nameof(StrongIdJsonConverterFactory)}.");
    }

    /// <summary>Converter for non-nullable StrongId values.</summary>
    private sealed class StrongIdConverter<TTag> : JsonConverter<StrongId<TTag>> where TTag : notnull
    {
        /// <inheritdoc />
        public override StrongId<TTag> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (StrongId<TTag>.TryParse(value, out var id))
            {
                return id;
            }

            throw new JsonException($"Invalid StrongId<{typeof(TTag).Name}> value: '{value}'");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, StrongId<TTag> value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value.ToString());
    }

    /// <summary>Converter for nullable StrongId values.</summary>
    private sealed class NullableStrongIdConverter<TTag> : JsonConverter<StrongId<TTag>?> where TTag : notnull
    {
        /// <inheritdoc />
        public override StrongId<TTag>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var value = reader.GetString();
            if (StrongId<TTag>.TryParse(value, out var id))
            {
                return id;
            }

            throw new JsonException($"Invalid StrongId<{typeof(TTag).Name}> value: '{value}'");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, StrongId<TTag>? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value.Value.ToString());
        }
    }
}
