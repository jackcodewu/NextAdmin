using NextAdmin.Application.DTOs.Bases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// Permission batch DTO
    /// </summary>
    public class PermissionsDto : BasesDto
    {      
        /// <summary>
        /// Permission Chinese name
        /// </summary>
        public string CnName { get; set; } = string.Empty;

        /// <summary>
        /// Permission code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Parent ID
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentId { get; set; }

        ///// <summary>
        ///// Child permissions
        ///// </summary>
        //public List<PermissionsDto> Children { get; set; } = new(); 
    }
} 
