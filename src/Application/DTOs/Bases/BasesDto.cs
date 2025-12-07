using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextAdmin.Application.DTOs
{
    [BsonIgnoreExtraElements] // 忽略数据库中存在但DTO中未定义的额外字段
    public class BasesDto : RootDto
    {
        /// <summary>
        /// 主键Id（字符串类型，兼容ObjectId）
        /// </summary>
        [BsonId] // ✅ 明确告诉驱动这是 _id 字段
        [BsonRepresentation(BsonType.ObjectId)]
        public virtual string? Id { get; set; }

        /// <summary>
        /// 公司名称
        /// </summary>
        public string? TenantName { get; set; }

        /// <summary>
        /// 创建时间（用于默认排序/游标）
        /// </summary>
        public virtual DateTime CreateTime { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public virtual DateTime UpdateTime { get; set; }
    }
}
