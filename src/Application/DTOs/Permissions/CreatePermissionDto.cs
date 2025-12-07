using NextAdmin.Application.DTOs.Bases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// 新增权限DTO
    /// </summary>
    public class CreatePermissionDto:CreateDto
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

        public List<CreatePermissionDto> Children { get; set; } = new();
    }
} 
