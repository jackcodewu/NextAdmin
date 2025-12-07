using System.Text.Json;

namespace NextAdmin.Common.Helpers
{
    /// <summary>
    /// 将 System.Text.Json 解析得到的 JsonElement / 原始 JSON 字符串 转换为 仅包含
    /// Dictionary<string, object>, List<object>, 基础类型(string/long/double/bool/null) 的纯净对象图，
    /// 以保证 MongoDB C# Driver 默认序列化器完全支持。
    /// </summary>
    public static class JsonDynamicConverter
    {
        /// <summary>
        /// 从原始 JSON 字符串解析并转换为纯净对象图。
        /// 若无效 JSON：返回一个包含 raw 字段的字典 { "raw" : 原文 }。
        /// 兼容可能的 PHP serialize(a:...) 字符串：包装为 { "phpSerialized": 原文 }
        /// </summary>
        public static object ParseToPlainObject(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new Dictionary<string, object>();
            }
            raw = raw.Trim();

            // PHP serialize 简单判定
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
        /// 将 JsonElement 递归转换为 Dictionary/List/primitive。
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
                    // 尝试进一步解析成 Guid / DateTime / DateTimeOffset / 数字 字符串? 这里保持原样，避免隐式格式改变
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
