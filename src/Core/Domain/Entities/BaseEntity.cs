using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Core.Domain.Entities
{
    /// <summary>
    /// Base entity class
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Entity ID
        /// </summary>
        public ObjectId Id { get; set; }

        /// <summary>
        /// Tenant ID - used for multi-tenant data isolation
        /// </summary>
        [BsonElement("TenantId")]
        public string? TenantId { get; set; }

        /// <summary>
        /// Creator ID
        /// </summary>
        public ObjectId CreatedById { get;private set; }

        /// <summary>
        /// Last updater ID
        /// </summary>
        public ObjectId UpdatedById { get; private set; }

        /// <summary>
        /// Creator name
        /// </summary>
        [BsonElement("CreatedByName")]
        public string? CreatedByName { get; private set; } = string.Empty;

        /// <summary>
        /// Last updater name
        /// </summary>
        [BsonElement("UpdatedByName")]
        public string? UpdatedByName { get; private set; } = string.Empty;

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("CreateTime")]
        public virtual DateTime CreateTime { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        [BsonElement("UpdateTime")]
        public virtual DateTime UpdateTime { get; set; }

        protected BaseEntity()
        {
            Id = ObjectId.GenerateNewId();
            CreateTime = DateTime.UtcNow;
            UpdateTime = DateTime.UtcNow;
        }

        protected BaseEntity(ObjectId id)
        {
            if (id == ObjectId.Empty)
                throw new ArgumentException("Entity Id cannot be empty", nameof(id));

            Id = id;
            CreateTime = DateTime.UtcNow;
            UpdateTime = DateTime.UtcNow;
        }

        public void SetCreatedById(ObjectId createdById)
        {
            if (createdById == ObjectId.Empty) throw new ArgumentException("createdById is empty.", nameof(createdById));

            if (CreatedById != ObjectId.Empty) return;

            CreatedById = createdById;
        }

        public void SetUpdatedById(ObjectId updatedById)
        {
            if (updatedById == ObjectId.Empty) throw new ArgumentException("updatedById is empty.", nameof(updatedById));

            UpdatedById = updatedById;
        }

        public void SetCreatedByName(string createdByName)
        {
            if (string.IsNullOrWhiteSpace(createdByName)) throw new ArgumentException("createdByName is null or empty.", nameof(createdByName));

            if (!string.IsNullOrWhiteSpace(CreatedByName)) return;

            CreatedByName = createdByName;
        }

        public void SetUpdatedByName(string updatedByName)
        {
            if (string.IsNullOrWhiteSpace(updatedByName)) throw new ArgumentException("updatedByName is null or empty.", nameof(updatedByName));

            UpdatedByName = updatedByName;
        }

        /// <summary>
        /// Set tenant ID
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        public void SetTenantId(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("tenantId is null or empty.", nameof(tenantId));

            TenantId = tenantId;
        }

        public void SetCreateTime()
        {
            CreateTime = DateTime.Now;
        }

        public void SetUpdateTime()
        {
            UpdateTime = DateTime.Now;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (obj.GetType() != GetType())
                return false;

            if (obj is not BaseEntity other)
                return false;

            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(BaseEntity left, BaseEntity right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static bool operator !=(BaseEntity left, BaseEntity right)
        {
            return !(left == right);
        }
    }
} 
