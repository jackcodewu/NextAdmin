using NextAdmin.Application.DTOs.Bases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// Create permission DTO
    /// </summary>
    public class CreatePermissionDto:CreateDto
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

        public List<CreatePermissionDto> Children { get; set; } = new();
    }
} 
