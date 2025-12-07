
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
namespace NextAdmin.API.Extensions
{

    public sealed class BsonDocumentJsonConverter : JsonConverter<BsonDocument>
    {
        public override BsonDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader); // 读取当前 JSON 值
            return BsonDocument.Parse(doc.RootElement.GetRawText()); // 直接转 BSON 文档
        }


        public override void Write(Utf8JsonWriter writer, BsonDocument value, JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.ToJson()); // 使用默认（Relaxed Extended JSON）
        }
    }

}
