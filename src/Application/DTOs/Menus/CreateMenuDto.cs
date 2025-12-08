using System.Collections.Generic;
using NextAdmin.Application.DTOs.Bases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Application.DTOs.Menus
{
    /// <summary>
    /// Create menu DTO
    /// </summary>
    public class CreateMenuDto : CreateDto
    {
        /// <summary>
        /// Path
        /// </summary>
        public string Path { get; set; } = string.Empty;
        
        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Icon
        /// </summary>
        public string Icon { get; set; } = string.Empty;
        
        /// <summary>
        /// Component
        /// </summary>
        public string Component { get; set; } = string.Empty;
        
        /// <summary>
        /// Parent ID (string type, compatible with ObjectId)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentId { get; set; }
        
        /// <summary>
        /// Is hidden
        /// </summary>
        public bool IsHide { get; set; }
        
        /// <summary>
        /// Keep alive
        /// </summary>
        public bool IsKeepAlive { get; set; }
        
        /// <summary>
        /// Is affix
        /// </summary>
        public bool IsAffix { get; set; }
        
        /// <summary>
        /// Link
        /// </summary>
        public string IsLink { get; set; } = string.Empty;
        
        /// <summary>
        /// Is iframe
        /// </summary>
        public bool IsIframe { get; set; }
        
        /// <summary>
        /// Sort order
        /// </summary>
        public int Sort { get; set; }
        
        /// <summary>
        /// Redirect
        /// </summary>
        public string? Redirect { get; set; }
    }
} 
