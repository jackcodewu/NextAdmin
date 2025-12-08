using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextAdmin.Application.DTOs
{
    [BsonIgnoreExtraElements] // Ignore extra fields that exist in the database but are not defined in the DTO
    public class BasesDto : RootDto
    {
        /// <summary>
        /// Primary key ID (string type, compatible with ObjectId)
        /// </summary>
        [BsonId] // ✅ Explicitly tells the driver this is the _id field
        [BsonRepresentation(BsonType.ObjectId)]
        public virtual string? Id { get; set; }

        /// <summary>
        /// Tenant name
        /// </summary>
        public string? TenantName { get; set; }

        /// <summary>
        /// Create time (for default sorting/cursor)
        /// </summary>
        public virtual DateTime CreateTime { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public virtual DateTime UpdateTime { get; set; }
    }
}
