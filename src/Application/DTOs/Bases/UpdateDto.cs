using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs.Bases
{
    /// <summary>
    /// 更新操作的基础DTO
    /// </summary>
    public class UpdateDto
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Required(ErrorMessage = "主键不能为空")]
        [StringLength(30, ErrorMessage = "主键长度不能超过30")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 名称
        /// </summary>
        [StringLength(100, ErrorMessage = "名称长度不能超过100")]
        public string? Name { get; set; }
    }
}
