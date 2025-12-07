using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using NextAdmin.Application.DTOs.Bases;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// 权限DTO
    /// </summary>
    public class PermissionDto : BaseDto
    {

        /// <summary>
        /// 权限编码（唯一）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? CnName { get; set; }

        /// <summary>
        /// 父级权限Id（支持权限树）
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentId { get; set; } = ObjectId.Empty.ToString();

        /// <summary>
        /// 父权限码
        /// </summary>
        public string ParentCode { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 子权限
        /// </summary>
        public List<PermissionDto> Children { get; set; } = new();
    }
} 
