using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NextAdmin.Application.DTOs.Bases;

namespace NextAdmin.Application.DTOs.Menus
{
    /// <summary>
    /// 菜单DTO
    /// </summary>
    public class MenuDto : BaseDto
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

        /// <summary>
        /// 子菜单
        /// </summary>
        public List<MenuDto> Children { get; set; } = new();
    }
} 
