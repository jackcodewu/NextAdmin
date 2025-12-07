using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Infrastructure.Configuration;

/// <summary>
/// MongoDB配置选项
/// </summary>
public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";
    
    [Required]
    public required string ConnectionString { get; set; }
    
    [Required]
    public required string DatabaseName { get; set; }
} 
