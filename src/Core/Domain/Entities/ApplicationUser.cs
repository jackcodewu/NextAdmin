using AspNetCore.Identity.Mongo.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Core.Domain.Entities;

public class ApplicationUser : MongoUser<ObjectId>
{
    public ObjectId TenantId { get; set; } = ObjectId.Empty;
    
    [MaxLength(100)]
    public string TenantName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FirstName { get; set; }
    
    [MaxLength(50)]
    public string? LastName { get; set; }
    
    [MaxLength(100)]
    public string? DisplayName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(100)]
    public string? Department { get; set; }
    
    [MaxLength(100)]
    public string? Position { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}
