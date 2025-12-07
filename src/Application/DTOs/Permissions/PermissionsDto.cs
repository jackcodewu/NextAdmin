using NextAdmin.Application.DTOs.Bases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// 权限批量DTO
    /// </summary>
    public class PermissionsDto : BasesDto
    {      
        /// <summary>
        /// 权限中文名称
        /// </summary>
        public string CnName { get; set; } = string.Empty;

        /// <summary>
        /// 权限码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 父级ID
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentId { get; set; }

        ///// <summary>
        ///// 子级权限
        ///// </summary>
        //public List<PermissionsDto> Children { get; set; } = new(); 
    }
} 
