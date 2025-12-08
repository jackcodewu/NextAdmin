using System.Text.Json;

namespace NextAdmin.Common.Helpers
{
    /// <summary>
    /// Converts JsonElement parsed by System.Text.Json or raw JSON string into a clean object graph
    /// containing only Dictionary<string, object>, List<object>, and primitive types (string/long/double/bool/null)
    /// to ensure full support by MongoDB C# Driver's default serializer.
    /// </summary>
    public static class JsonDynamicConverter
    {
        /// <summary>
        /// Parse raw JSON string and convert to clean object graph.
        /// For invalid JSON: returns a dictionary with raw field { "raw" : original text }.
        /// Compatible with possible PHP serialize(a:...) strings: wraps as { "phpSerialized": original text }
        /// </summary>
        public static object ParseToPlainObject(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new Dictionary<string, object>();
            }
            raw = raw.Trim();

            // Simple PHP serialize detection
            if (raw.StartsWith("a:") && raw.Contains(";"))
            {
                return new Dictionary<string, object>
                {
                    ["phpSerialized"] = raw
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(raw);
                return ToPlainObject(doc.RootElement);
            }
            catch
            {
                return new Dictionary<string, object>
                {
                    ["raw"] = raw
                };
            }
        }

        /// <summary>
        /// Recursively convert JsonElement to Dictionary/List/primitive.
        /// </summary>
        public static object? ToPlainObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ToPlainObject(prop.Value);
                    }
                    return dict;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ToPlainObject(item));
                    }
                    return list;
                case JsonValueKind.String:
                    // Try to further parse as Guid / DateTime / DateTimeOffset / numeric strings? Keep as-is here to avoid implicit format changes
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l)) return l;
                    if (element.TryGetDouble(out double d)) return d;
                    return element.GetRawText();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    return null;
            }
        }
    }
}
