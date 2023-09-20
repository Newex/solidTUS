using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

using SolidTUS.Models.JsonDeserializers;

namespace SolidTUS.Attributes;

/// <summary>
/// Custom converter for <see cref="ReadOnlyDictionary{TKey, TValue}"/>
/// </summary>
public class JsonReadOnlyDictionaryAttribute : JsonConverterAttribute
{
    /// <inheritdoc />
    public JsonReadOnlyDictionaryAttribute() : base(typeof(JsonReadOnlyDictionaryConverter))
    {
    }
}
