using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextAdmin.Core.Domain.Interfaces.Repositories
{
    /// <summary>
    /// MongoDB generic repository interface
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IBaseRepository<TEntity> where TEntity : AggregateRoot
    {
        /// <summary>
        /// Whether caching is supported
        /// </summary>
        bool SupportsCaching { get; set; }

        /// <summary>
        /// Cache expiration time
        /// </summary>
        TimeSpan CacheExpiry { get; set; }

        /// <summary>
        /// Get cached object
        /// </summary>
        Task<T?> GetCacheObjectAsync<T>(string cacheKey);

        /// <summary>
        /// Set cached object
        /// </summary>
        Task SetCacheObjectAsync<T>(string cacheKey, T value, TimeSpan? expiry = null);

        /// <summary>
        /// Remove specified cache
        /// </summary>
        Task RemoveCacheObjectAsync(string cacheKey);

        /// <summary>
        /// Remove cache by prefix
        /// </summary>
        Task RemoveCacheByPrefixAsync(string cacheKeyPrefix);

        // 1. Get
        /// <summary>
        /// Get entity by ID and company ID
        /// </summary>
        Task<TEntity> GetByIdAsync(ObjectId id);

        /// <summary>
        /// Get all entities
        /// </summary>
        Task<List<TEntity>> GetAllAsync(ObjectId TenantId);

        /// <summary>
        /// Get all entities (with sorting)
        /// </summary>
        Task<List<TEntity>> GetAllAsync(ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false);

        /// <summary>
        /// Conditional query (with company ID)
        /// </summary>
        Task<List<TEntity>> GetAsync(FilterDefinition<TEntity> filter, ObjectId TenantId);

        /// <summary>
        /// Conditional query (with company ID and sorting)
        /// </summary>
        Task<List<TEntity>> GetAsync(FilterDefinition<TEntity> filter, ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false);

    /// <summary>
    /// Conditional query with direct projection to specified type (with company ID/sorting)
    /// Uses MongoDB projection if provided, otherwise attempts to return result as TProjection
    /// </summary>
    Task<List<TProjection>> GetAsync<TProjection>(FilterDefinition<TEntity> filter, ObjectId TenantId, ProjectionDefinition<TEntity, TProjection>? projection = null, string sortField = "CreateTime", bool isAsc = false);

        /// <summary>
        /// Get paginated data (with company ID)
        /// </summary>
        Task<(List<TEntity> Items, long Total)> GetListPageAsync(int pageNumber, int pageSize, FilterDefinition<TEntity>? expression, ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false);

        // 2. Add
        /// <summary>
        /// Add entity
        /// </summary>
        /// <param name="TEntity">Entity</param>
        /// <returns>Added entity</returns>
        Task<TEntity> AddAsync(TEntity TEntity, bool hasEmpty = false);

        /// <summary>
        /// Batch add entities
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entities">Entity list</param>
        /// <returns>Number of successfully added entities</returns>
        Task<int> AddManyAsync(List<TEntity> entities, bool hasEmpty = false);

        // 3. Update
        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="TEntity">Entity</param>
        /// <returns>Updated entity</returns>
        Task<TEntity> UpdateAsync(ObjectId id, TEntity TEntity, bool hasEmpty = false);

        /// <summary>
        /// Update entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="TEntity">Entity</param>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateAsync(TEntity TEntity, bool hasEmpty = false);

        /// <summary>
        /// Batch update entities
        /// </summary>
        /// <param name="entities">Entity list</param>
        Task UpdateManyAsync(List<TEntity> entities, bool hasEmpty = false);

        // 4. Delete
        /// <summary>
        /// Delete entity (with company ID)
        /// </summary>
        Task<bool> DeleteAsync(ObjectId id);

        // 5. Count
        /// <summary>
        /// Get entity count (with company ID)
        /// </summary>
        Task<long> CountAsync(FilterDefinition<TEntity> filter, ObjectId TenantId);

        /// <summary>
        /// Get single entity (with company ID)
        /// </summary>
        Task<TEntity?> GetOneAsync(FilterDefinition<TEntity> filter, ObjectId TenantId);

        // Other helper methods
        /// <summary>
        /// Get collection name
        /// </summary>
        /// <returns>Collection name</returns>
        string GetCollectionName();
        Task<long> CountAsync(ObjectId TenantId);
        Task<(List<TProjection> Items, long Total)> GetListPageAsync<TProjection>(int pageNumber, int pageSize, FilterDefinition<TEntity>? expression, ObjectId TenantId, ProjectionDefinition<TEntity, TProjection>? projection = null, string sortField = "CreateTime", bool isAsc = false);
    }
}



