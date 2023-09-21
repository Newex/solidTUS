using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using SolidTUS.Extensions;

namespace SolidTUS.Models.JsonDeserializers;

/// <summary>
/// Converter for <see cref="ReadOnlyDictionary{TKey, TValue}"/>
/// </summary>
/// <remarks>source: https://stackoverflow.com/a/70813056/1640121</remarks>
public class JsonReadOnlyDictionaryConverter: JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                    && (!typeToConvert.IsGenericType || typeToConvert.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>) ||
                    typeof(ReadOnlyDictionary<,>).IsSubclassOfRawGeneric(typeToConvert));
    }

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var iReadOnlyDictionary = typeToConvert.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
        Type keyType = iReadOnlyDictionary.GetGenericArguments()[0];
        Type valueType = iReadOnlyDictionary.GetGenericArguments()[1];

        var converter = Activator.CreateInstance(
            typeof(ReadOnlyDictionaryConverterInner<,>).MakeGenericType(keyType, valueType),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null, args: null, culture: null) as JsonConverter;

        return converter;
    }

    private class ReadOnlyDictionaryConverterInner<TKey, TValue> : JsonConverter<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
    {
        public override IReadOnlyDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionary = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options: options);

            return dictionary == null
                ? null
                : Activator.CreateInstance(
                    typeToConvert, BindingFlags.Instance | BindingFlags.Public,
                    binder: null, args: new object[] { dictionary }, culture: null) as IReadOnlyDictionary<TKey, TValue>;
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<TKey, TValue> dictionary, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, dictionary, options);
    }
}
