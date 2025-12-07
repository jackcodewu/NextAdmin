using System.Collections.Generic;
using NextAdmin.Core.Domain.Events;
using MongoDB.Bson;
using MediatR;

namespace NextAdmin.Core.Domain.Entities
{
    /// <summary>
    /// 聚合根基类
    /// </summary>
    public abstract class AggregateRoot : BaseEntity
    {
        private readonly List<INotification> _domainEvents = new();

        protected AggregateRoot() : base()
        {
        }

        protected AggregateRoot(ObjectId id) : base(id)
        {
            Id = id;
        }

        /// <summary>
        /// 领域事件集合
        /// </summary>
        public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();


        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; protected set; } = true;

        /// <summary>
        /// 是否已删除
        /// </summary>
        public bool IsDeleted { get; protected set; }

        /// <summary>
        /// 添加领域事件
        /// </summary>
        /// <param name="domainEvent">领域事件</param>
        protected void AddDomainEvent(INotification domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// 移除领域事件
        /// </summary>
        /// <param name="domainEvent">领域事件</param>
        protected void RemoveDomainEvent(INotification domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        /// <summary>
        /// 清除所有领域事件
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        public void SetDeleted()
        {
            IsDeleted = true;
            IsEnabled = false;
        }

        public void SetEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

    }
} 
