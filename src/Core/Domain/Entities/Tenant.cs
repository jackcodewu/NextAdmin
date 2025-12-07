using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Core.Domain.Entities
{
    /// <summary>
    /// 租户实体
    /// </summary>
    public class Tenant : AggregateRoot
    {
        /// <summary>
        /// 租户编码（唯一标识）
        /// </summary>
        [BsonElement("Code")]
        public string Code { get; private set; } = string.Empty;

        /// <summary>
        /// 租户描述
        /// </summary>
        [BsonElement("Description")]
        public string? Description { get; private set; }

        /// <summary>
        /// 联系人
        /// </summary>
        [BsonElement("ContactPerson")]
        public string? ContactPerson { get; private set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [BsonElement("ContactPhone")]
        public string? ContactPhone { get; private set; }

        /// <summary>
        /// 联系邮箱
        /// </summary>
        [BsonElement("ContactEmail")]
        public string? ContactEmail { get; private set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        [BsonElement("ExpirationDate")]
        public DateTime? ExpirationDate { get; private set; }

        /// <summary>
        /// 最大用户数限制
        /// </summary>
        [BsonElement("MaxUserCount")]
        public int? MaxUserCount { get; private set; }

        /// <summary>
        /// 自定义配置（JSON格式）
        /// </summary>
        [BsonElement("CustomConfig")]
        public string? CustomConfig { get; private set; }

        private Tenant() { }

        public Tenant(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("租户编码不能为空", nameof(code));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("租户名称不能为空", nameof(name));

            Code = code;
            Name = name;
            SetEnabled(true);
        }

        /// <summary>
        /// 更新租户信息
        /// </summary>
        public void UpdateInfo(string name, string? description, string? contactPerson, 
            string? contactPhone, string? contactEmail)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("租户名称不能为空", nameof(name));

            Name = name;
            Description = description;
            ContactPerson = contactPerson;
            ContactPhone = contactPhone;
            ContactEmail = contactEmail;
            SetUpdateTime();
        }

        /// <summary>
        /// 设置过期时间
        /// </summary>
        public void SetExpirationDate(DateTime? expirationDate)
        {
            ExpirationDate = expirationDate;
            SetUpdateTime();
        }

        /// <summary>
        /// 设置最大用户数
        /// </summary>
        public void SetMaxUserCount(int? maxUserCount)
        {
            if (maxUserCount.HasValue && maxUserCount.Value < 0)
                throw new ArgumentException("最大用户数不能小于0", nameof(maxUserCount));

            MaxUserCount = maxUserCount;
            SetUpdateTime();
        }

        /// <summary>
        /// 设置自定义配置
        /// </summary>
        public void SetCustomConfig(string? customConfig)
        {
            CustomConfig = customConfig;
            SetUpdateTime();
        }

        /// <summary>
        /// 检查租户是否已过期
        /// </summary>
        public bool IsExpired()
        {
            return ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
        }

        /// <summary>
        /// 检查租户是否可用
        /// </summary>
        public bool IsAvailable()
        {
            return IsEnabled && !IsExpired();
        }
    }
}
