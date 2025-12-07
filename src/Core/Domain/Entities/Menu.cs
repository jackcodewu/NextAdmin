using NextAdmin.Core.Domain.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Core.Domain.Entities.Sys
{
    /// <summary>
    /// 系统菜单实体
    /// </summary>
    [BsonDiscriminator(RootClass = true)]
    [MongoCollection("menus")]
    public class Menu : AggregateRoot
    {

        /// <summary>
        /// URL路径
        /// </summary>
        public string? Path { get; set; } = string.Empty;

        /// <summary>
        /// 中文名称
        /// </summary>
        public string? Title { get; set; } = string.Empty;

        /// <summary>
        /// 图标
        /// </summary>
        public string? Icon { get; set; } = string.Empty;

        /// <summary>
        /// 组件地址
        /// </summary>
        public string? Component { get; set; } = string.Empty;

        /// <summary>
        /// 父级菜单Id
        /// </summary>
        [BsonElement("ParentId")]
        public ObjectId? ParentId { get; set; } = ObjectId.Empty;

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool IsHide { get; set; } = false;

        /// <summary>
        /// 是否缓存组件状态
        /// </summary>
        public bool IsKeepAlive { get; set; } = true;

        /// <summary>
        /// 是否固定在tagsView栏
        /// </summary>
        public bool IsAffix { get; set; } = false;

        /// <summary>
        /// 超链接菜单
        /// </summary>
        public string? IsLink { get; set; } = string.Empty;

        /// <summary>
        /// 是否内嵌
        /// </summary>
        public bool IsIframe { get; set; } = false;

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; } = 0;

        /// <summary>
        /// 重定向
        /// </summary>
        public string? Redirect { get; set; } = string.Empty;

        /// <summary>
        /// 子菜单列表，用于存储树形结构
        /// </summary>
        [BsonIgnoreIfNull]
        public List<Menu> Children { get; set; } = new List<Menu>();

        public Menu DeepClone()
        {
            return new Menu(ObjectId.GenerateNewId())
            {
                Id = this.Id,
                Path = this.Path,
                Title = this.Title,
                Name = this.Name,
                Icon = this.Icon,
                Component = this.Component,
                ParentId = this.ParentId,
                IsHide = this.IsHide,
                IsKeepAlive = this.IsKeepAlive,
                IsAffix = this.IsAffix,
                IsLink = this.IsLink,
                IsIframe = this.IsIframe,
                Sort = this.Sort,
                Redirect = this.Redirect,
                Children = this.Children?.Select(c => c.DeepClone()).ToList(),
            };
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Menu() : base()
        {
            // 默认值已在属性声明中初始化
        }

        public Menu(ObjectId id) : base(id)
        {
            Children = new List<Menu>();
        }
    }
} 
