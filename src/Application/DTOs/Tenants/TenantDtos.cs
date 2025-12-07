using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Core.Domain.Entities;
using MongoDB.Driver;

namespace NextAdmin.Application.DTOs.Tenants
{
    /// <summary>
    /// 租户基础DTO
    /// </summary>
    public class TenantDto : BaseDto
    {
        /// <summary>
        /// 租户编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 租户名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 租户描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 联系人
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string? ContactPhone { get; set; }

        /// <summary>
        /// 联系邮箱
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// 最大用户数限制
        /// </summary>
        public int? MaxUserCount { get; set; }

        /// <summary>
        /// 自定义配置
        /// </summary>
        public string? CustomConfig { get; set; }

        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// 创建租户DTO
    /// </summary>
    public class CreateTenantDto : CreateDto
    {
        /// <summary>
        /// 租户编码
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// 租户名称
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// 租户描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 联系人
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string? ContactPhone { get; set; }

        /// <summary>
        /// 联系邮箱
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// 最大用户数限制
        /// </summary>
        public int? MaxUserCount { get; set; }

        /// <summary>
        /// 自定义配置
        /// </summary>
        public string? CustomConfig { get; set; }
    }

    /// <summary>
    /// 更新租户DTO
    /// </summary>
    public class UpdateTenantDto : UpdateDto
    {
        /// <summary>
        /// 租户名称
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// 租户描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 联系人
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string? ContactPhone { get; set; }

        /// <summary>
        /// 联系邮箱
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// 最大用户数限制
        /// </summary>
        public int? MaxUserCount { get; set; }

        /// <summary>
        /// 自定义配置
        /// </summary>
        public string? CustomConfig { get; set; }
    }

    /// <summary>
    /// 租户查询DTO
    /// </summary>
    public class TenantQueryDto : QueryDto<Tenant>
    {
        /// <summary>
        /// 租户编码或名称（模糊搜索）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public new bool? IsEnabled { get; set; }

        /// <summary>
        /// 是否包含过期租户
        /// </summary>
        public bool IncludeExpired { get; set; } = true;

        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? SortField { get; set; } = "CreateTime";

        /// <summary>
        /// 是否升序
        /// </summary>
        public bool IsAscending { get; set; } = false;

        public override FilterDefinition<Tenant> ToExpression()
        {
            var filters = new List<FilterDefinition<Tenant>>();

            // 关键字搜索（租户编码或名称）
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var keywordFilter = Builders<Tenant>.Filter.Or(
                    Builders<Tenant>.Filter.Regex(t => t.Code, new MongoDB.Bson.BsonRegularExpression(Keyword, "i")),
                    Builders<Tenant>.Filter.Regex(t => t.Name, new MongoDB.Bson.BsonRegularExpression(Keyword, "i"))
                );
                filters.Add(keywordFilter);
            }

            // 是否启用
            if (IsEnabled.HasValue)
            {
                filters.Add(Builders<Tenant>.Filter.Eq(t => t.IsEnabled, IsEnabled.Value));
            }

            // 是否包含过期租户
            if (!IncludeExpired)
            {
                var now = DateTime.UtcNow;
                var notExpiredFilter = Builders<Tenant>.Filter.Or(
                    Builders<Tenant>.Filter.Eq(t => t.ExpirationDate, null),
                    Builders<Tenant>.Filter.Gt(t => t.ExpirationDate, now)
                );
                filters.Add(notExpiredFilter);
            }

            return filters.Count > 0 
                ? Builders<Tenant>.Filter.And(filters) 
                : Builders<Tenant>.Filter.Empty;
        }
    }

    /// <summary>
    /// 租户批量操作DTO（BasesDto）
    /// </summary>
    public class TenantsDto : BasesDto
    {
        // 如需扩展批量操作字段，可在此添加
    }
}
