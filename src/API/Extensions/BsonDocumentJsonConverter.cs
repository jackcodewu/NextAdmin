
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
            using var doc = JsonDocument.ParseValue(ref reader); // Read current JSON value
            return BsonDocument.Parse(doc.RootElement.GetRawText()); // Convert directly to BSON document
        }


        public override void Write(Utf8JsonWriter writer, BsonDocument value, JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.ToJson()); // Use default (Relaxed Extended JSON)
        }
    }

}
