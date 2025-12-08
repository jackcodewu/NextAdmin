using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DTOs
{
    [BsonIgnoreExtraElements] // Ignore extra fields that exist in the database but are not defined in the DTO
    public class RootDto
    {
        /// <summary>
        /// Tenant ID (string type, compatible with ObjectId)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? TenantId { get; set; }


        /// <summary>
        /// Name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Is enabled
        /// </summary>
        public bool IsEnabled { get;  set; } = true;
    }
}
