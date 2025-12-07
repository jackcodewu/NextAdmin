using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DTOs
{
    [BsonIgnoreExtraElements] // 忽略数据库中存在但DTO中未定义的额外字段
    public class RootDto
    {
        /// <summary>
        /// 公司ID（字符串类型，兼容ObjectId）
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? TenantId { get; set; }


        /// <summary>
        /// 名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get;  set; } = true;
    }
}
