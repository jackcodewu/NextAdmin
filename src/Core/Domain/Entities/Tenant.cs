using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Core.Domain.Entities
{
    /// <summary>
    /// Tenant entity
    /// </summary>
    public class Tenant : AggregateRoot
    {
        /// <summary>
        /// Tenant code (unique identifier)
        /// </summary>
        [BsonElement("Code")]
        public string Code { get; private set; } = string.Empty;

        /// <summary>
        /// Tenant description
        /// </summary>
        [BsonElement("Description")]
        public string? Description { get; private set; }

        /// <summary>
        /// Contact person
        /// </summary>
        [BsonElement("ContactPerson")]
        public string? ContactPerson { get; private set; }

        /// <summary>
        /// Contact phone
        /// </summary>
        [BsonElement("ContactPhone")]
        public string? ContactPhone { get; private set; }

        /// <summary>
        /// Contact email
        /// </summary>
        [BsonElement("ContactEmail")]
        public string? ContactEmail { get; private set; }

        /// <summary>
        /// Expiration date
        /// </summary>
        [BsonElement("ExpirationDate")]
        public DateTime? ExpirationDate { get; private set; }

        /// <summary>
        /// Maximum user count limit
        /// </summary>
        [BsonElement("MaxUserCount")]
        public int? MaxUserCount { get; private set; }

        /// <summary>
        /// Custom configuration (JSON format)
        /// </summary>
        [BsonElement("CustomConfig")]
        public string? CustomConfig { get; private set; }

        private Tenant() { }

        public Tenant(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Tenant code cannot be empty", nameof(code));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tenant name cannot be empty", nameof(name));

            Code = code;
            Name = name;
            SetEnabled(true);
        }

        /// <summary>
        /// Update tenant information
        /// </summary>
        public void UpdateInfo(string name, string? description, string? contactPerson, 
            string? contactPhone, string? contactEmail)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tenant name cannot be empty", nameof(name));

            Name = name;
            Description = description;
            ContactPerson = contactPerson;
            ContactPhone = contactPhone;
            ContactEmail = contactEmail;
            SetUpdateTime();
        }

        /// <summary>
        /// Set expiration date
        /// </summary>
        public void SetExpirationDate(DateTime? expirationDate)
        {
            ExpirationDate = expirationDate;
            SetUpdateTime();
        }

        /// <summary>
        /// Set maximum user count
        /// </summary>
        public void SetMaxUserCount(int? maxUserCount)
        {
            if (maxUserCount.HasValue && maxUserCount.Value < 0)
                throw new ArgumentException("Maximum user count cannot be less than 0", nameof(maxUserCount));

            MaxUserCount = maxUserCount;
            SetUpdateTime();
        }

        /// <summary>
        /// Set custom configuration
        /// </summary>
        public void SetCustomConfig(string? customConfig)
        {
            CustomConfig = customConfig;
            SetUpdateTime();
        }

        /// <summary>
        /// Check if tenant is expired
        /// </summary>
        public bool IsExpired()
        {
            return ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
        }

        /// <summary>
        /// Check if tenant is available
        /// </summary>
        public bool IsAvailable()
        {
            return IsEnabled && !IsExpired();
        }
    }
}
