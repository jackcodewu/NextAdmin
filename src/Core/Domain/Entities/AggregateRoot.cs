using System.Collections.Generic;
using NextAdmin.Core.Domain.Events;
using MongoDB.Bson;
using MediatR;

namespace NextAdmin.Core.Domain.Entities
{
    /// <summary>
    /// Aggregate root base class
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
        /// Domain events collection
        /// </summary>
        public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();


        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether enabled
        /// </summary>
        public bool IsEnabled { get; protected set; } = true;

        /// <summary>
        /// Whether deleted
        /// </summary>
        public bool IsDeleted { get; protected set; }

        /// <summary>
        /// Add domain event
        /// </summary>
        /// <param name="domainEvent">Domain event</param>
        protected void AddDomainEvent(INotification domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Remove domain event
        /// </summary>
        /// <param name="domainEvent">Domain event</param>
        protected void RemoveDomainEvent(INotification domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        /// <summary>
        /// Clear all domain events
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
