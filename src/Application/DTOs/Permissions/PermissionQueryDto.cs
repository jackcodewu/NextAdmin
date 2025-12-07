using System;
using System.Linq.Expressions;
using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// 权限查询DTO
    /// </summary>
    public class PermissionQueryDto : QueryPageDto<PermissionQueryDto, Permission>
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
    public string? ParentCode { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Sort { get; set; }

        public override FilterDefinition<Permission> ToExpression()
        {
            var builder = Builders<Permission>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrEmpty(Name))
            {
                filter &= builder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(Name, "i"));
            }
            if (!string.IsNullOrEmpty(CnName))
            {
                filter &= builder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(CnName, "i"));
            }
            if (!string.IsNullOrEmpty(Code))
            {
                filter &= builder.Regex(x => x.Code, new MongoDB.Bson.BsonRegularExpression(Code, "i"));
            }
            if (!string.IsNullOrEmpty(ParentId) && ObjectId.TryParse(ParentId, out var parentObjectId))
            {
                filter &= builder.Eq(x => x.ParentId, parentObjectId);
            }

            
            filter &= base.ToExpression();

            return filter;
        }
    }
} 
