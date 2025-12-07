using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextAdmin.API.Extensions.Json
{
    /// <summary>
    /// 将 DateTime 统一序列化为 "yyyy-MM-dd HH:mm:ss"，反序列化支持常见格式。
    /// UTC 输入将转换为本地时间再输出。
    /// </summary>
    public sealed class DateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _format;
        public DateTimeConverter(string? format = null)
        {
            _format = string.IsNullOrWhiteSpace(format) ? "yyyy-MM-dd HH:mm:ss" : format!;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str)) return default;

                // 优先尝试精确格式，其次尝试多种常见格式与通用解析
                if (DateTime.TryParseExact(str, _format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                    return dt;

                if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                    return dt;
            }

            // 退回默认
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var dt = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
            writer.WriteStringValue(dt.ToString(_format));
        }
    }

    /// <summary>
    /// 可空 DateTime 的转换器。
    /// </summary>
    public sealed class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        private readonly DateTimeConverter _inner;
        public NullableDateTimeConverter(string? format = null)
        {
            _inner = new DateTimeConverter(format);
        }

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return _inner.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }
            _inner.Write(writer, value.Value, options);
        }
    }
}
