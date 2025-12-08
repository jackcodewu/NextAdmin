using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Entities.Sys;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace NextAdmin.Application.DTOs.Roles
{
    /// <summary>
    /// Role query DTO
    /// </summary>
    public class RoleQueryDto
    {
        // public int PageNumber { get; set; } = 1;
        // public int PageSize { get; set; } = 20;

        public string? Description { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string? TenantId { get; set; }
        public string? Name { get;  set; }

        public Expression<Func<ApplicationRole, bool>> ToExpression()
        {
            // Pre-convert ObjectId to avoid string conversion in the database
            bool TenantIdParsed = ObjectId.TryParse(TenantId, out ObjectId TenantObjectId);

            return r =>
                (string.IsNullOrEmpty(Name) || r.Name.Contains(Name)) &&
                (string.IsNullOrEmpty(Description) || (r.Description != null && r.Description.Contains(Description))) &&
                (!TenantIdParsed || r.TenantId == TenantObjectId);
        }
    }
} 
