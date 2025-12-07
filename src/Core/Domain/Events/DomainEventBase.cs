using NextAdmin.Core.Domain.Entities;
using MediatR;
using MongoDB.Bson;
using System;

namespace NextAdmin.Core.Domain.Events
{
    /// <summary>
    /// 抽象领域事件
    /// </summary>
    public abstract class DomainEventBase<TEntity> : INotification where TEntity : AggregateRoot
    {
        public ObjectId Id { get; set; }
        public DateTime OccurredOn { get; }
        public TEntity Entity { get; protected set; }
        public DomainEventType DomainEventType { get; }
        public int Version { get; }

        protected DomainEventBase(TEntity entity, DomainEventType domainEventType)
        {
            Id = entity.Id;
            Entity = entity;
            DomainEventType = domainEventType;
            OccurredOn = DateTime.UtcNow;
            Version = 1;
        }
    }
}
