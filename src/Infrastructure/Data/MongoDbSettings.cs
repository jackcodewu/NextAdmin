using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Infrastructure.Configuration;

/// <summary>
/// MongoDB configuration options
/// </summary>
public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";
    
    [Required]
    public required string ConnectionString { get; set; }
    
    [Required]
    public required string DatabaseName { get; set; }
} 
