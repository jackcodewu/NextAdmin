using System.Collections.Generic;
using NextAdmin.Application.DTOs.Bases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Application.DTOs.Menus
{
    /// <summary>
    /// Update menu DTO
    /// </summary>
    public class UpdateMenuDto : UpdateDto
    {
        
        public string Path { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Component { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentId { get; set; }
        public bool IsHide { get; set; }
        public bool IsKeepAlive { get; set; }
        public bool IsAffix { get; set; }
        public string IsLink { get; set; }
        public bool IsIframe { get; set; }
        public int Sort { get; set; }
        public string? Redirect { get; set; }
    }
} 
