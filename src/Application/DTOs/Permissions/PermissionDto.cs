using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using NextAdmin.Application.DTOs.Bases;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// Permission DTO
    /// </summary>
    public class PermissionDto : BaseDto
    {

        /// <summary>
        /// Permission code (unique)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Permission description
        /// </summary>
        public string? CnName { get; set; }

        /// <summary>
        /// Parent permission ID (supports permission tree)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentId { get; set; } = ObjectId.Empty.ToString();

        /// <summary>
        /// Parent permission code
        /// </summary>
        public string ParentCode { get; set; }

        /// <summary>
        /// Sort order
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// Child permissions
        /// </summary>
        public List<PermissionDto> Children { get; set; } = new();
    }
} 
