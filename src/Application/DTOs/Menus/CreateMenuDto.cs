using System.Collections.Generic;
using NextAdmin.Application.DTOs.Bases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Application.DTOs.Menus
{
    /// <summary>
    /// 新增菜单DTO
    /// </summary>
    public class CreateMenuDto : CreateDto
    {
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; } = string.Empty;
        
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; } = string.Empty;
        
        /// <summary>
        /// 组件
        /// </summary>
        public string Component { get; set; } = string.Empty;
        
        /// <summary>
        /// 父级ID（字符串类型，兼容ObjectId）
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentId { get; set; }
        
        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool IsHide { get; set; }
        
        /// <summary>
        /// 是否保持活跃
        /// </summary>
        public bool IsKeepAlive { get; set; }
        
        /// <summary>
        /// 是否固定
        /// </summary>
        public bool IsAffix { get; set; }
        
        /// <summary>
        /// 链接
        /// </summary>
        public string IsLink { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否iframe
        /// </summary>
        public bool IsIframe { get; set; }
        
        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }
        
        /// <summary>
        /// 重定向
        /// </summary>
        public string? Redirect { get; set; }
    }
} 
