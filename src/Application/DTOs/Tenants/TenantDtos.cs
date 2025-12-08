using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Core.Domain.Entities;
using MongoDB.Driver;

namespace NextAdmin.Application.DTOs.Tenants
{
    /// <summary>
    /// Tenant base DTO
    /// </summary>
    public class TenantDto : BaseDto
    {
        /// <summary>
        /// Tenant code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Tenant name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tenant description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Contact person
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// Contact phone
        /// </summary>
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Contact email
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Expiration date
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Maximum user count limit
        /// </summary>
        public int? MaxUserCount { get; set; }

        /// <summary>
        /// Custom configuration
        /// </summary>
        public string? CustomConfig { get; set; }

        /// <summary>
        /// Is expired
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// Is available
        /// </summary>
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// Create tenant DTO
    /// </summary>
    public class CreateTenantDto : CreateDto
    {
        /// <summary>
        /// Tenant code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Tenant name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Tenant description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Contact person
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// Contact phone
        /// </summary>
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Contact email
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Expiration date
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Maximum user count limit
        /// </summary>
        public int? MaxUserCount { get; set; }

        /// <summary>
        /// Custom configuration
        /// </summary>
        public string? CustomConfig { get; set; }
    }

    /// <summary>
    /// Update tenant DTO
    /// </summary>
    public class UpdateTenantDto : UpdateDto
    {
        /// <summary>
        /// Tenant name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Tenant description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Contact person
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// Contact phone
        /// </summary>
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Contact email
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Expiration date
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Maximum user count limit
        /// </summary>
        public int? MaxUserCount { get; set; }

        /// <summary>
        /// Custom configuration
        /// </summary>
        public string? CustomConfig { get; set; }
    }

    /// <summary>
    /// Tenant query DTO
    /// </summary>
    public class TenantQueryDto : QueryDto<Tenant>
    {
        /// <summary>
        /// Tenant code or name (fuzzy search)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Is enabled
        /// </summary>
        public new bool? IsEnabled { get; set; }

        /// <summary>
        /// Include expired tenants
        /// </summary>
        public bool IncludeExpired { get; set; } = true;

        /// <summary>
        /// Page number
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Items per page
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort field
        /// </summary>
        public string? SortField { get; set; } = "CreateTime";

        /// <summary>
        /// Is ascending
        /// </summary>
        public bool IsAscending { get; set; } = false;

        public override FilterDefinition<Tenant> ToExpression()
        {
            var filters = new List<FilterDefinition<Tenant>>();

            // Keyword search (tenant code or name)
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var keywordFilter = Builders<Tenant>.Filter.Or(
                    Builders<Tenant>.Filter.Regex(t => t.Code, new MongoDB.Bson.BsonRegularExpression(Keyword, "i")),
                    Builders<Tenant>.Filter.Regex(t => t.Name, new MongoDB.Bson.BsonRegularExpression(Keyword, "i"))
                );
                filters.Add(keywordFilter);
            }

            // Is enabled
            if (IsEnabled.HasValue)
            {
                filters.Add(Builders<Tenant>.Filter.Eq(t => t.IsEnabled, IsEnabled.Value));
            }

            // Include expired tenants
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
    /// Tenant batch operation DTO (BasesDto)
    /// </summary>
    public class TenantsDto : BasesDto
    {
        // Add batch operation fields here if needed
    }
}
