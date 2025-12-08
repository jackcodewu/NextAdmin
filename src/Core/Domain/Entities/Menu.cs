using NextAdmin.Core.Domain.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Core.Domain.Entities.Sys
{
    /// <summary>
    /// System menu entity
    /// </summary>
    [BsonDiscriminator(RootClass = true)]
    [MongoCollection("menus")]
    public class Menu : AggregateRoot
    {

        /// <summary>
        /// URL path
        /// </summary>
        public string? Path { get; set; } = string.Empty;

        /// <summary>
        /// Chinese name
        /// </summary>
        public string? Title { get; set; } = string.Empty;

        /// <summary>
        /// Icon
        /// </summary>
        public string? Icon { get; set; } = string.Empty;

        /// <summary>
        /// Component address
        /// </summary>
        public string? Component { get; set; } = string.Empty;

        /// <summary>
        /// Parent menu ID
        /// </summary>
        [BsonElement("ParentId")]
        public ObjectId? ParentId { get; set; } = ObjectId.Empty;

        /// <summary>
        /// Whether to hide
        /// </summary>
        public bool IsHide { get; set; } = false;

        /// <summary>
        /// Whether to cache component state
        /// </summary>
        public bool IsKeepAlive { get; set; } = true;

        /// <summary>
        /// Whether to pin in tagsView bar
        /// </summary>
        public bool IsAffix { get; set; } = false;

        /// <summary>
        /// Hyperlink menu
        /// </summary>
        public string? IsLink { get; set; } = string.Empty;

        /// <summary>
        /// Whether to embed
        /// </summary>
        public bool IsIframe { get; set; } = false;

        /// <summary>
        /// Sort order
        /// </summary>
        public int Sort { get; set; } = 0;

        /// <summary>
        /// Redirect
        /// </summary>
        public string? Redirect { get; set; } = string.Empty;

        /// <summary>
        /// Child menu list, used to store tree structure
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
        /// Default constructor
        /// </summary>
        public Menu() : base()
        {
            // Default values are initialized in property declarations
        }

        public Menu(ObjectId id) : base(id)
        {
            Children = new List<Menu>();
        }
    }
} 
