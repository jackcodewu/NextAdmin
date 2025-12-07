using System;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using MongoDB.Driver;

namespace NextAdmin.Application.DTOs.Menus
{
    /// <summary>
    /// 菜单查询DTO，支持分页和条件过滤
    /// </summary>
    public class MenuQueryDto: QueryPageDto<MenuQueryDto, Menu>
    {
    // 页码
    // public int PageNumber { get; set; } = 1;

    // 每页大小
    // public int PageSize { get; set; } = 20;

        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentId { get; set; }
        public bool? IsHide { get; set; }
        public string? Title { get; set; }
        public string? Path { get; set; }

        public override FilterDefinition<Menu> ToExpression()
        {
            var builder = Builders<Menu>.Filter;
            var filter = builder.Empty;

            if (ObjectId.TryParse(ParentId, out var parentObjectId))
            {
                filter &= builder.Eq(x => x.ParentId, parentObjectId);
            }
            if (!string.IsNullOrEmpty(Name))
            {
                filter &= builder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(Name, "i"));
            }
            if (!string.IsNullOrEmpty(Title))
            {
                filter &= builder.Regex(x => x.Title, new MongoDB.Bson.BsonRegularExpression(Title, "i"));
            }
            if (!string.IsNullOrEmpty(Path))
            {
                filter &= builder.Regex(x => x.Path, new MongoDB.Bson.BsonRegularExpression(Path, "i"));
            }
            if (IsHide.HasValue)
            {
                filter &= builder.Eq(x => x.IsHide, IsHide.Value);
            }

            
            {
                filter &= base.ToExpression();
            }   

            return filter;
        }
    }
} 
