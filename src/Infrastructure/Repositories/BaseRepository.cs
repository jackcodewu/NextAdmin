using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Extensions;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Log;
using NextAdmin.Redis;
using NextAdmin.Shared.Common;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SharpCompress.Common;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization;

namespace NextAdmin.Infrastructure.Repositories
{
    /// <summary>
    /// MongoDB repository base class
    /// Provides basic CRUD operations and collection access
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    public partial class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : AggregateRoot
    {
        #region Partial Update Configuration
        /// <summary>
        /// Property reflection cache (static, shared by all instances)
        /// </summary>
        private static readonly PropertyInfo[] CachedProperties;
        private static readonly HashSet<string> SkipPropertyNames = new() { "Id", "UpdateTime", "VIPCommands,CreatedById", "CreatedByName", "CreateTime" };

        /// <summary>
        /// Check if property has BsonIgnore attribute
        /// </summary>
        private static bool HasBsonIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<MongoDB.Bson.Serialization.Attributes.BsonIgnoreAttribute>() != null;
        }

        /// <summary>
        /// Determine if type is not supported for MongoDB serialization
        /// </summary>
        private static bool IsUnsupportedType(Type type)
        {
            // Skip concurrent collection types like ConcurrentQueue
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(System.Collections.Concurrent.ConcurrentQueue<>) ||
                    genericTypeDef == typeof(System.Collections.Concurrent.ConcurrentBag<>) ||
                    genericTypeDef == typeof(System.Collections.Concurrent.ConcurrentStack<>) ||
                    genericTypeDef == typeof(System.Collections.Concurrent.ConcurrentDictionary<,>))
                {
                    return true;
                }
            }
            return false;
        }
        // 3. Update
        /// <summary>
        /// Determine if property value is valid (not default value)
        /// </summary>
        private static bool HasValue(object? value)
        {
            if (value == null)
                return false;

            return value switch
            {
                string strValue => !string.IsNullOrWhiteSpace(strValue),
                ObjectId objectIdValue => objectIdValue != ObjectId.Empty,
                DateTime dateTimeValue => dateTimeValue != default,
                System.Collections.ICollection collection => collection.Count >= 0, // Collection type: only non-empty collections are considered to have value
                bool _ => true, // bool type: both false and true are valid values, should not be skipped
                _ when value.GetType().IsValueType => !value.Equals(Activator.CreateInstance(value.GetType())),
                _ => true // Other reference types and not null
            };
        }

        /// <summary>
        /// Build update definitions (only include properties with values)
        /// </summary>
        private static List<UpdateDefinition<TEntity>> BuildUpdateDefinitions(TEntity entity)
        {
            var updateDefinitionList = new List<UpdateDefinition<TEntity>>
            {
                // Always update UpdateTime
                Builders<TEntity>.Update.Set(x => x.UpdateTime, DateTime.UtcNow)
            };

            // Use cached property information
            foreach (var property in CachedProperties)
            {
                var value = property.GetValue(entity);

                // Only add to update definition if it has value
                if (HasValue(value))
                {
                    var updateDef = Builders<TEntity>.Update.Set(property.Name, value);
                    updateDefinitionList.Add(updateDef);
                }
            }

            return updateDefinitionList;
        } 
        #endregion

        #region Fields
        /// <summary>
        /// MongoDB database instance
        /// </summary>
        protected readonly IMongoDatabase Database;
        private readonly string collectionName;
        protected readonly IRedisService? _redisService;
        protected readonly string _key;

        private bool _supportsCaching = false;
        /// <summary>
        /// Whether supports caching
        /// </summary>
        public virtual bool SupportsCaching
        {
            get => _supportsCaching;
            set { _supportsCaching = value; }
        }

        /// <summary>
        /// Cache expiry time
        /// </summary>
        public virtual TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Entity collection
        /// </summary>
        protected readonly IMongoCollection<TEntity> Collection; 
        #endregion

        #region Constructor 
        /// <summary>
        /// Static constructor, initialize property cache
        /// </summary>
        static BaseRepository()
        {
            CachedProperties = typeof(TEntity)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    p.CanWrite &&
                    !SkipPropertyNames.Contains(p.Name) &&
                    !HasBsonIgnoreAttribute(p) &&
                    !IsUnsupportedType(p.PropertyType))
                .ToArray();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="database">MongoDB database instance</param>
        /// <param name="collectionName">Collection name</param>
        public BaseRepository(IMongoDatabase database, IRedisService? redisService = null, bool isUnique = false, string? key = null, bool isCache = true)
        {
            Database = database;
            var attr = typeof(TEntity).GetCustomAttribute<MongoCollectionAttribute>();
            var collectionName = attr?.Name ?? $"{typeof(TEntity).Name}s";
            this.collectionName = collectionName;
            Collection = database.GetCollection<TEntity>(collectionName);
            _redisService = redisService;
            _key = string.IsNullOrWhiteSpace(key) ? typeof(TEntity).Name : key;
            CreateIndexes();
            CreateNameIndex(isUnique);
            _supportsCaching = _redisService != null;
        }
        #endregion
        
        #region Create Indexes

        protected void CreateNameIndex(bool isUnique)
        {
            // Name unique index
            var nameIndex = Builders<TEntity>.IndexKeys.Ascending(x => x.Name);
            Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(nameIndex, new CreateIndexOptions { Unique = isUnique }));
        }

        private void CreateIndexes()
        {
            try
            {

                // CreateTime descending index
                var createTimeIndex = Builders<TEntity>.IndexKeys.Descending(x => x.CreateTime);
                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(createTimeIndex));

                // CreateTime + _id composite index (descending) - for seek pagination
                var createTimeIdDescIndex = Builders<TEntity>.IndexKeys
                    .Descending(x => x.CreateTime)
                    .Descending(x => x.Id);
                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(createTimeIdDescIndex,
                    new CreateIndexOptions { Name = "CreateTime_Id_desc" }));

                // CreateTime + _id composite index (ascending) - for seek pagination
                var createTimeIdAscIndex = Builders<TEntity>.IndexKeys
                    .Ascending(x => x.CreateTime)
                    .Ascending(x => x.Id);
                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(createTimeIdAscIndex,
                    new CreateIndexOptions { Name = "CreateTime_Id_asc" }));

                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Ascending(x => x.UpdateTime)));

                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Ascending(x => x.CreatedById)));

                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Ascending(x => x.UpdatedById)));

                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Ascending(x => x.CreatedByName)));

                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Ascending(x => x.UpdatedByName)));
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("An existing index has the same name"))
            {
                // Index already exists, ignore error
                LogHelper.Info($"Index already exists, skipping creation: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while creating index: {ex.Message}");
                throw;
            }
        } 
        #endregion

        #region Add Functionality

        // 2. Add
        /// <summary>
        /// Add entity
        /// </summary>
        /// <param name="TEntity">Entity</param>
        /// <returns>Added entity</returns>
        public virtual async Task<TEntity> AddAsync(TEntity TEntity, bool hasEmpty = false)
        {
            try
            {

                await Collection.InsertOneAsync(TEntity);

                if (SupportsCaching)
                {
                    await DelCache();
                    var cacheKey = $"{_key}:{TEntity.Id}";
                    await SetCacheObjectAsync(cacheKey, TEntity);
                }

                return TEntity;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Batch add entities
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entities">Entity list</param>
        /// <returns>Number of successfully added entities</returns>
        public virtual async Task<int> AddManyAsync(List<TEntity> entities, bool hasEmpty = false)
        {
            try
            {
                if (entities == null || !entities.Any())
                {
                    return 0;
                }

                var collection = Database.GetCollection<TEntity>(collectionName);
                await collection.InsertManyAsync(entities);
                await DelCache();
                return entities.Count();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        } 
        #endregion

        #region Update Functionality

        /// <summary>
        /// Update entity (only update non-empty properties)
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="TEntity">Entity</param>
        /// <param name="hasEmpty">Whether to allow empty TenantId</param>
        /// <returns>Updated entity</returns>
        public virtual async Task<TEntity> UpdateAsync(ObjectId id, TEntity TEntity, bool hasEmpty = false)
        {
            try
            {

                // Build update definition
                var updateDefinitionList = BuildUpdateDefinitions(TEntity);

                // If no fields need updating, return original entity
                if (updateDefinitionList.Count == 0)
                    return await GetByIdAsync(id);

                // Combine all update definitions
                var updateDefinition = Builders<TEntity>.Update.Combine(updateDefinitionList);

                var options = new FindOneAndUpdateOptions<TEntity>
                {
                    ReturnDocument = ReturnDocument.After
                };

                var updatedEntity = await Collection.FindOneAndUpdateAsync<TEntity>(
                    x => x.Id == id,
                    updateDefinition,
                    options
                );

                if (SupportsCaching && updatedEntity != null)
                {
                    await DelCache();
                    var cacheKey = $"{_key}:{id}";
                    await SetCacheObjectAsync(cacheKey, updatedEntity);
                }

                return updatedEntity;
            }
            catch (Exception err)
            {
                throw new Exception(err.Message, err);
            }
        }

        /// <summary>
        /// Update entity (only update non-empty properties)
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="TEntity">Entity</param>
        /// <param name="hasEmpty">Whether to allow empty TenantId</param>
        /// <returns>Whether successful</returns>
        public virtual async Task<bool> UpdateAsync(TEntity TEntity, bool hasEmpty = false)
        {
            try
            {

                // Build update definition
                var updateDefinitionList = BuildUpdateDefinitions(TEntity);

                // If no fields need updating, return directly
                if (updateDefinitionList.Count == 0)
                    return true;

                // Combine all update definitions
                var updateDefinition = Builders<TEntity>.Update.Combine(updateDefinitionList);

                var collection = Database.GetCollection<TEntity>(collectionName);
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, TEntity.Id);
                var result = await collection.UpdateOneAsync(filter, updateDefinition);

                if (SupportsCaching && result.ModifiedCount > 0)
                {
                    await DelCache();
                    // Re-fetch complete entity and cache
                    var updatedEntity = await GetByIdAsync(TEntity.Id);
                    if (updatedEntity != null)
                    {
                        var cacheKey = $"{_key}:{TEntity.Id}";
                        await SetCacheObjectAsync(cacheKey, updatedEntity);
                    }
                }

                return result.ModifiedCount > 0;
            }
            catch (Exception err)
            {
                throw new Exception(err.Message, err);
            }
        }

        /// <summary>
        /// Batch update entities (only update non-empty properties)
        /// </summary>
        /// <param name="entities">Entity list</param>
        /// <param name="hasEmpty">Whether to allow empty TenantId</param>
        public virtual async Task UpdateManyAsync(List<TEntity> entities, bool hasEmpty = false)
        {
            try
            {
                if (entities == null || !entities.Any())
                    return;

                var bulkOps = new List<WriteModel<TEntity>>();
                foreach (var entity in entities)
                {
                    // Use optimized method to build update definition
                    var updateDefinitionList = BuildUpdateDefinitions(entity);

                    // If there are fields to update, add to batch operation
                    if (updateDefinitionList.Count > 0)
                    {
                        var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
                        var updateDefinition = Builders<TEntity>.Update.Combine(updateDefinitionList);
                        var updateModel = new UpdateOneModel<TEntity>(filter, updateDefinition) { IsUpsert = false };
                        bulkOps.Add(updateModel);
                    }
                }

                if (bulkOps.Count > 0)
                {
                    await Collection.BulkWriteAsync(bulkOps);
                    await DelCache();
                }
            }
            catch (Exception err)
            {
                throw new Exception(err.Message, err);
            }
        }
        #endregion


        #region Query Functionality
        /// <summary>
        /// Generate base filter with TenantId
        /// </summary>
        protected FilterDefinition<TEntity> BuildTenantFilter(ObjectId TenantId)
        {
            var filter = Builders<TEntity>.Filter.Eq(x => x.IsDeleted, false);

            return filter;
        }

        // 1. Get
        /// <summary>
        /// Get entity by ID and company ID
        /// </summary>
        public virtual async Task<TEntity> GetByIdAsync(ObjectId id)
        {
            if (SupportsCaching)
            {
                var entity = await _redisService.GetObjectAsync<TEntity>($"{_key}:{id}");
                if (entity != null)
                    return entity;
            }

            var filter = Builders<TEntity>.Filter.Eq(x => x.Id, id) & Builders<TEntity>.Filter.Eq(x => x.IsDeleted, false);
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get single entity (with company ID)
        /// </summary>
        public virtual async Task<TEntity?> GetOneAsync(FilterDefinition<TEntity> filter, ObjectId TenantId)
        {
            var baseFilter = BuildTenantFilter(TenantId) & filter;

            var cachekey = GenerateCacheKey($"{_key}:one", baseFilter);
            if (SupportsCaching)
            {
                var entity = await _redisService.GetObjectAsync<TEntity>(cachekey);
                if (entity != null)
                    return entity;
            }

            if (SupportsCaching)
            {
                var entity = await Collection.Find(baseFilter).FirstOrDefaultAsync();
                if (entity != null)
                {
                    await SetCacheObjectAsync(cachekey, entity);
                }
                return entity;
            }

            return await Collection.Find(baseFilter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get all entities (with company ID)
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllAsync(ObjectId TenantId)
        {
            // Generate cache key (use fixed key for parameterless method)
            var cacheKey = $"{_key}:all";

            // Try to get from cache
            if (SupportsCaching)
            {
                var entities = await _redisService.GetObjectAsync<List<TEntity>>(cacheKey);
                if (entities != null && entities.Count > 0)
                    return entities;

                var allEntities = await Collection.Find(BuildTenantFilter(TenantId)).ToListAsync();

                if (allEntities != null && allEntities.Count > 0)
                {
                    await SetCacheObjectAsync(cacheKey, allEntities);
                }

                return allEntities;
            }

            return await Collection.Find(BuildTenantFilter(TenantId)).ToListAsync();
        }

        /// <summary>
        /// Get all entities (with company ID and sorting)
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllAsync(ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false)
        {
            // Generate cache key (use fixed key for parameterless method)
            var cachekey = $"{_key}:all:e";

            // Try to get from cache
            if (SupportsCaching)
            {
                var cachedDtos = await _redisService.GetObjectAsync<List<TEntity>>(cachekey);
                if (cachedDtos != null && cachedDtos.Count > 0)
                    return cachedDtos;
            }

            var query = Collection.Find(BuildTenantFilter(TenantId));
            if (!string.IsNullOrEmpty(sortField))
            {
                var sortDefinition = isAsc
                    ? Builders<TEntity>.Sort.Ascending(sortField)
                    : Builders<TEntity>.Sort.Descending(sortField);
                query = query.Sort(sortDefinition);
            }

            var entities = await query.ToListAsync();

            if (SupportsCaching && entities?.Any() == true)
            {
                await SetCacheObjectAsync(cachekey, entities);
            }

            return entities;
        }

        /// <summary>
        /// Conditional query (with company ID)
        /// </summary>
        public virtual async Task<List<TEntity>> GetAsync(FilterDefinition<TEntity> predicate, ObjectId TenantId)
        {

            var filter = predicate & BuildTenantFilter(TenantId);
            var cachekey = GenerateCacheKey($"{_key}:list", filter);

            if (SupportsCaching)
            {
                var entities = await _redisService.GetObjectAsync<List<TEntity>>(cachekey);
                if (entities != null && entities.Count > 0)
                    return entities;
            }

            var allEntities = await Collection.Find(filter).ToListAsync();
            if (SupportsCaching && allEntities?.Any() == true)
            {
                await SetCacheObjectAsync(cachekey, allEntities);
            }
            return allEntities;
        }

        /// <summary>
        /// Conditional query (with company ID and sorting)
        /// </summary>
        public virtual async Task<List<TEntity>> GetAsync(FilterDefinition<TEntity> predicate, ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false)
        {

            var filter = predicate & BuildTenantFilter(TenantId);
            var cachekey = GenerateCacheKey($"{_key}:list:sort", filter);

            if (SupportsCaching)
            {
                var cachentities = await _redisService.GetObjectAsync<List<TEntity>>(cachekey);
                if (cachentities != null && cachentities.Count > 0)
                    return cachentities;
            }

            var query = Collection.Find(filter);
            if (!string.IsNullOrEmpty(sortField))
            {
                var sortDefinition = isAsc
                    ? Builders<TEntity>.Sort.Ascending(sortField)
                    : Builders<TEntity>.Sort.Descending(sortField);
                query = query.Sort(sortDefinition);
            }
            var entities = await query.ToListAsync();

            if (SupportsCaching && entities?.Any() == true)
            {
                await SetCacheObjectAsync(cachekey, entities);
            }

            return entities;
        }

        /// <summary>
        /// Conditional query with direct projection to specified type (with company ID and sorting)
        /// </summary>
        public virtual async Task<List<TProjection>> GetAsync<TProjection>(FilterDefinition<TEntity> predicate, ObjectId TenantId, ProjectionDefinition<TEntity, TProjection>? projection = null, string sortField = "CreateTime", bool isAsc = false)
        {
            try
            {
                var filter = predicate & BuildTenantFilter(TenantId);
                var cachekey = GenerateCacheKey($"{_key}:list:proj", filter);

                if (SupportsCaching)
                {
                    var cached = await _redisService.GetObjectAsync<List<TProjection>>(cachekey);
                    if (cached != null && cached.Count > 0)
                        return cached;
                }

                var query = Collection.Find(filter);
                if (!string.IsNullOrEmpty(sortField))
                {
                    var sortDefinition = isAsc
                        ? Builders<TEntity>.Sort.Ascending(sortField)
                        : Builders<TEntity>.Sort.Descending(sortField);
                    query = query.Sort(sortDefinition);
                }

                List<TProjection> items;
                if (projection != null)
                {
                    items = await query.Project(projection).ToListAsync();
                }
                else
                {
                    items = await query.As<TProjection>().ToListAsync();
                }

                if (SupportsCaching && items?.Any() == true)
                {
                    await SetCacheObjectAsync(cachekey, items);
                }

                return items;
            }
            catch (Exception ex)
            {
                return new List<TProjection>();
            }
        }

        /// <summary>
        /// Conditional query (using MongoDB native filter, with company ID and sorting)
        /// </summary>
        public virtual async Task<List<TEntity>> GetAsync(FilterDefinition<TEntity> filter, string sortField = "CreateTime", bool isAsc = false)
        {

            var baseFilter = BuildTenantFilter(ObjectId.Empty);
            if (filter != null)
                baseFilter &= filter;


            // Generate cache key (use fixed key for parameterless method)
            var cacheKey = GenerateCacheKey($"{_key}:sort", baseFilter);

            // Try to get from cache
            if (SupportsCaching)
            {
                var cachentities = await _redisService.GetObjectAsync<List<TEntity>>(cacheKey);
                if (cachentities != null && cachentities.Count > 0)
                    return cachentities;
            }


            var query = Collection.Find(baseFilter);
            if (!string.IsNullOrEmpty(sortField))
            {
                var sortDefinition = isAsc
                    ? Builders<TEntity>.Sort.Ascending(sortField)
                    : Builders<TEntity>.Sort.Descending(sortField);
                query = query.Sort(sortDefinition);
            }

            var entities = await query.ToListAsync();

            if (SupportsCaching && entities?.Any() == true)
            {
                await SetCacheObjectAsync(cacheKey, entities);
            }

            return entities;
        }

        /// <summary>
        /// Get paginated data (using Seek cursor pagination, supports CreateTime + _id sorting)
        /// Cursor information saved to Redis, supports random page jumping
        /// </summary>
        public virtual async Task<(List<TEntity> Items, long Total)> GetListPageAsync(
            int pageNumber, int pageSize, FilterDefinition<TEntity>? expression, ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var filter = BuildTenantFilter(TenantId);
            if (expression != null)
                filter &= expression;

            // Generate cursor cache key (including filter conditions and sorting)
            var filterHash = GenerateFilterCacheKey(filter);
            var sortDirection = isAsc ? "asc" : "desc";
            var cursorCacheKeyPrefix = $"{_key}:cursor:{filterHash}:{sortField}:{sortDirection}";
            var dataCacheKey = $"{cursorCacheKeyPrefix}:page:{pageNumber}:{pageSize}";

            // Try to get data from cache
            if (SupportsCaching)
            {
                var cached = await GetCacheObjectAsync<(List<TEntity> Items, long Total)>(dataCacheKey);
                if (cached.Items != null && cached.Items.Count > 0)
                    return cached;
            }

            // Get total count (parallel)
            var totalTask = CountAsync(filter, TenantId);

            // Get or calculate cursor position
            string cursorKey = $"{cursorCacheKeyPrefix}:page:{pageNumber - 1}";
            SeekCursor? cursor = null;

            if (pageNumber > 1 && SupportsCaching)
            {
                cursor = await GetCacheObjectAsync<SeekCursor>(cursorKey);
            }

            // Build query
            var seekFilter = filter;
            if (cursor != null)
            {
                // Continue query using cursor
                if (isAsc)
                {
                    seekFilter &= Builders<TEntity>.Filter.Or(
                        Builders<TEntity>.Filter.Gt(sortField, cursor.SortValue),
                        Builders<TEntity>.Filter.And(
                            Builders<TEntity>.Filter.Eq(sortField, cursor.SortValue),
                            Builders<TEntity>.Filter.Gt(x => x.Id, cursor.Id)
                        )
                    );
                }
                else
                {
                    seekFilter &= Builders<TEntity>.Filter.Or(
                        Builders<TEntity>.Filter.Lt(sortField, cursor.SortValue),
                        Builders<TEntity>.Filter.And(
                            Builders<TEntity>.Filter.Eq(sortField, cursor.SortValue),
                            Builders<TEntity>.Filter.Lt(x => x.Id, cursor.Id)
                        )
                    );
                }
            }

            // Build sorting
            var sortDefinition = isAsc
                ? Builders<TEntity>.Sort.Ascending(sortField).Ascending(x => x.Id)
                : Builders<TEntity>.Sort.Descending(sortField).Descending(x => x.Id);

            // Execute query
            var query = Collection.Find(seekFilter).Sort(sortDefinition).Limit(pageSize);
            var items = await query.ToListAsync();
            var total = await totalTask;

            // Save current page cursor to Redis (for next page)
            if (SupportsCaching && items.Count > 0)
            {
                var lastItem = items[items.Count - 1];
                var sortValue = GetSortFieldValue(lastItem, sortField);
                var nextCursor = new SeekCursor
                {
                    Id = lastItem.Id,
                    SortValue = sortValue,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var nextCursorKey = $"{cursorCacheKeyPrefix}:page:{pageNumber}";
                await SetCacheObjectAsync(nextCursorKey, nextCursor, TimeSpan.FromMinutes(30));

                // Cache data result
                await SetCacheObjectAsync(dataCacheKey, (items, total), TimeSpan.FromMinutes(10));
            }

            return (items, total);
        }

        /// <summary>
        /// Get paginated data (using Seek cursor pagination + MongoDB projection, returns lightweight DTO)
        /// Cursor information saved to Redis, supports random page jumping
        /// Suitable for list queries, avoids loading large fields (e.g., Commands)
        /// </summary>
        /// <typeparam name="TProjection">Projection type (lightweight DTO)</typeparam>
        // ===================== Inside BaseRepository<TEntity> class =====================

        public virtual async Task<(List<TProjection> Items, long Total)> GetListPageAsync<TProjection>(
            int pageNumber,
            int pageSize,
            FilterDefinition<TEntity>? expression,
            ObjectId TenantId,
            ProjectionDefinition<TEntity, TProjection>? projection = null,
            string sortField = "CreateTime",
            bool isAsc = false)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var filter = BuildTenantFilter(TenantId);
            if (expression != null)
                filter &= expression;

            // Generate cache key (including filter conditions/sorting/projection) - consistent with original implementation
            var filterHash = GenerateFilterCacheKey(filter);
            var sortDirection = isAsc ? "asc" : "desc";
            var projectionHash = projection?.ToString()?.GetHashCode().ToString() ?? "full";
            var cursorCacheKeyPrefix = $"{_key}:cursor:{filterHash}:{sortField}:{sortDirection}:{projectionHash}";
            var dataCacheKey = $"{cursorCacheKeyPrefix}:page:{pageNumber}:{pageSize}";

            // First try to hit full page cache
            if (SupportsCaching)
            {
                var cached = await GetCacheObjectAsync<(List<TProjection> Items, long Total)>(dataCacheKey);
                if (cached.Items != null && cached.Items.Count > 0)
                    return cached;
            }

            // Calculate total count in parallel
            var totalTask = CountAsync(filter, TenantId);

            // Read previous page cursor (for Seek)
            string cursorKey = $"{cursorCacheKeyPrefix}:page:{pageNumber - 1}";
            SeekCursor? cursor = null;

            if (pageNumber > 1 && SupportsCaching)
            {
                cursor = await GetCacheObjectAsync<SeekCursor>(cursorKey);
            }

            // ====== ✅ Fallback: when previous page cursor is missing, use Skip/Limit to get this page directly ======
            if (pageNumber > 1 && SupportsCaching && cursor is null)
            {
                var sortForSkip = isAsc
                    ? Builders<TEntity>.Sort.Ascending(sortField).Ascending(x => x.Id)
                    : Builders<TEntity>.Sort.Descending(sortField).Descending(x => x.Id);

                var skip = (pageNumber - 1) * pageSize;
                var find2 = Collection.Find(filter).Sort(sortForSkip).Skip(skip).Limit(pageSize);

                List<TProjection> items2;
                if (projection != null)
                    items2 = await find2.Project(projection).ToListAsync();
                else
                    items2 = await find2.As<TProjection>().ToListAsync();

                var total2 = await totalTask;

                // Write back "current page" cursor & cache current page data (for next page/refresh)
                if (SupportsCaching && items2.Count > 0)
                {
                    var last = items2[^1];
                    var lastId = TryGetIdFromProjection(last);
                    var lastSortValue = TryGetSortValueFromProjection(last, sortField);

                    if (lastId.HasValue && lastSortValue != null)
                    {
                        var nextCursor = new SeekCursor
                        {
                            Id = lastId.Value,
                            SortValue = lastSortValue,
                            PageNumber = pageNumber,
                            PageSize = pageSize
                        };

                        var nextCursorKey = $"{cursorCacheKeyPrefix}:page:{pageNumber}";
                        await SetCacheObjectAsync(nextCursorKey, nextCursor, TimeSpan.FromMinutes(30));
                        await SetCacheObjectAsync(dataCacheKey, (items2, total2), TimeSpan.FromMinutes(10));
                    }
                }

                return (items2, total2);
            }
            // ====== ✅ Fallback ends, normal Seek path continues ======

            // Build Seek filter
            var seekFilter = filter;
            if (cursor != null)
            {
                if (isAsc)
                {
                    seekFilter &= Builders<TEntity>.Filter.Or(
                        Builders<TEntity>.Filter.Gt(sortField, cursor.SortValue),
                        Builders<TEntity>.Filter.And(
                            Builders<TEntity>.Filter.Eq(sortField, cursor.SortValue),
                            Builders<TEntity>.Filter.Gt(x => x.Id, cursor.Id)
                        )
                    );
                }
                else
                {
                    seekFilter &= Builders<TEntity>.Filter.Or(
                        Builders<TEntity>.Filter.Lt(sortField, cursor.SortValue),
                        Builders<TEntity>.Filter.And(
                            Builders<TEntity>.Filter.Eq(sortField, cursor.SortValue),
                            Builders<TEntity>.Filter.Lt(x => x.Id, cursor.Id)
                        )
                    );
                }
            }

            // Sorting: primary sort + Id as stable secondary sort
            var sortDefinition = isAsc
                ? Builders<TEntity>.Sort.Ascending(sortField).Ascending(x => x.Id)
                : Builders<TEntity>.Sort.Descending(sortField).Descending(x => x.Id);

            // Execute query (with optional projection)
            var findBase = Collection.Find(seekFilter).Sort(sortDefinition).Limit(pageSize);

            List<TProjection> items;
            ObjectId? lastId2 = null;
            BsonValue? lastSortValue2 = null;

            if (projection != null)
            {
                items = await findBase.Project(projection).ToListAsync();

                if (items.Count > 0)
                {
                    var lastItem = items[^1];
                    lastId2 = TryGetIdFromProjection(lastItem);
                    lastSortValue2 = TryGetSortValueFromProjection(lastItem, sortField);
                }
            }
            else
            {
                items = await findBase.As<TProjection>().ToListAsync();

                if (items.Count > 0)
                {
                    var lastItem = items[^1];
                    lastId2 = TryGetIdFromProjection(lastItem);
                    lastSortValue2 = TryGetSortValueFromProjection(lastItem, sortField);
                }
            }

            var total = await totalTask;

            // Save cursor and data for current page
            if (SupportsCaching && items.Count > 0 && lastId2.HasValue && lastSortValue2 != null)
            {
                var nextCursor = new SeekCursor
                {
                    Id = lastId2.Value,
                    SortValue = lastSortValue2,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var nextCursorKey = $"{cursorCacheKeyPrefix}:page:{pageNumber}";
                await SetCacheObjectAsync(nextCursorKey, nextCursor, TimeSpan.FromMinutes(30));
                await SetCacheObjectAsync(dataCacheKey, (items, total), TimeSpan.FromMinutes(10));
            }

            return (items, total);
        }


        /// <summary>
        /// Extract Id from projection object
        /// </summary>
        private ObjectId? TryGetIdFromProjection<T>(T item)
        {
            if (item == null) return null;

            var type = typeof(T);
            var idProp = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (idProp != null)
            {
                var val = idProp.GetValue(item);
                if (val is ObjectId oid) return oid;
                if (val is string sid && ObjectId.TryParse(sid, out var o1)) return o1;
            }
            return null;
        }

        /// <summary>
        /// Extract sort field value from projection object
        /// </summary>
        private BsonValue? TryGetSortValueFromProjection<T>(T item, string fieldName)
        {
            if (item == null) return null;

            try
            {
                var type = typeof(T);
                var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null) return null;

                var value = prop.GetValue(item);
                if (value == null) return BsonNull.Value;

                // Convert to BsonValue
                if (value is DateTime dt) return new BsonDateTime(dt);
                if (value is string str) return new BsonString(str);
                if (value is int intVal) return new BsonInt32(intVal);
                if (value is long longVal) return new BsonInt64(longVal);
                if (value is double dblVal) return new BsonDouble(dblVal);
                if (value is ObjectId objId) return new BsonObjectId(objId);

                return BsonValue.Create(value);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Cursor information class
        /// </summary>
        private class SeekCursor
        {
            public ObjectId Id { get; set; }
            public BsonValue SortValue { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }

        /// <summary>
        /// Get sort field value through reflection
        /// </summary>
        private BsonValue GetSortFieldValue(TEntity entity, string fieldName)
        {
            try
            {
                var property = typeof(TEntity).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null)
                    return BsonNull.Value;

                var value = property.GetValue(entity);
                if (value == null)
                    return BsonNull.Value;

                // Convert to BsonValue
                if (value is DateTime dt)
                    return new BsonDateTime(dt);
                if (value is string str)
                    return new BsonString(str);
                if (value is int intVal)
                    return new BsonInt32(intVal);
                if (value is long longVal)
                    return new BsonInt64(longVal);
                if (value is double dblVal)
                    return new BsonDouble(dblVal);
                if (value is ObjectId objId)
                    return new BsonObjectId(objId);

                // Default conversion
                return BsonValue.Create(value);
            }
            catch
            {
                return BsonNull.Value;
            }
        }

        // 5. Count
        /// <summary>
        /// Get entity count (with company ID)
        /// </summary>
        public async Task<long> CountAsync(FilterDefinition<TEntity> filter, ObjectId TenantId)
        {

            var baseFilter = BuildTenantFilter(TenantId);
            if (filter != null)
                baseFilter &= filter;

            var cacheKey = GenerateCacheKey($"{_key}:count", baseFilter);

            // Try to get from cache
            if (SupportsCaching)
            {
                var cached = await GetCacheObjectAsync<long?>(cacheKey);
                if (cached.HasValue && cached.Value > 0)
                    return cached.Value;
            }

            var count = await Collection.CountDocumentsAsync(baseFilter);

            if (SupportsCaching && count > 0)
                await SetCacheObjectAsync(cacheKey, count);

            return count;
        }

        public async Task<long> CountAsync(ObjectId TenantId)
        {

            var baseFilter = BuildTenantFilter(TenantId);

            var cacheKey = GenerateCacheKey($"{_key}:count:all", baseFilter);

            // Try to get from cache
            if (SupportsCaching)
            {
                var cached = await GetCacheObjectAsync<long?>(cacheKey);
                if (cached.HasValue && cached.Value > 0)
                    return cached.Value;
            }

            var count = await Collection.CountDocumentsAsync(baseFilter);

            if (SupportsCaching && count > 0)
                await SetCacheObjectAsync(cacheKey, count);

            return count;
        }
        #endregion

        #region Delete Functionality

        // 4. Delete
        /// <summary>
        /// Delete entity (with company ID)
        /// </summary>
        public virtual async Task<bool> DeleteAsync(ObjectId id)
        {
            var entity = await GetByIdAsync(id);
            if (entity is null) return false;
            entity.SetDeleted();
            if (SupportsCaching)
            {
                await DelCache();
                var cachekey = $"{_key}:{id}";
                await _redisService.SetObjectAsync(cachekey, entity);
            }
            return await UpdateAsync(entity);
        }

        #endregion

        #region Cache Related
        // Other helper methods
        /// <summary>
        /// Get collection name
        /// </summary>
        /// <returns>Collection name</returns>
        public string GetCollectionName()
        {
            return collectionName;
        }

        /// <summary>
        /// Get cache value
        /// </summary>
        public virtual async Task<T?> GetCacheObjectAsync<T>(string cacheKey)
        {
            if (!SupportsCaching || string.IsNullOrWhiteSpace(cacheKey))
                return default;

            try
            {
                return await _redisService!.GetObjectAsync<T>(cacheKey);
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"Failed to get cache: {cacheKey}, {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Set cache value
        /// </summary>
        public virtual async Task SetCacheObjectAsync<T>(string cacheKey, T value, TimeSpan? expiry = null)
        {
            if (!SupportsCaching || string.IsNullOrWhiteSpace(cacheKey))
                return;

            try
            {
                var effectiveExpiry = expiry ?? CacheExpiry;
                await _redisService!.SetObjectAsync(cacheKey, value, effectiveExpiry);
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"Failed to set cache: {cacheKey}, {ex.Message}");
            }
        }

        /// <summary>
        /// Delete cache
        /// </summary>
        protected async Task DelCache(string? _key = null)
        {
            if (string.IsNullOrWhiteSpace(_key))
                _key = this._key;

            if (SupportsCaching)
            {
                await RemoveCacheByPrefixAsync(_key);
            }
        }

        /// <summary>
        /// Delete specified cache
        /// </summary>
        public virtual async Task RemoveCacheObjectAsync(string cacheKey)
        {
            if (!SupportsCaching || string.IsNullOrWhiteSpace(cacheKey))
                return;

            try
            {
                await _redisService!.DeleteAsync(cacheKey);
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"Failed to delete cache: {cacheKey}, {ex.Message}");
            }
        }

        /// <summary>
        /// Batch delete cache by prefix
        /// </summary>
        public virtual async Task RemoveCacheByPrefixAsync(string cacheKeyPrefix)
        {
            if (!SupportsCaching || string.IsNullOrWhiteSpace(cacheKeyPrefix))
                return;

            try
            {
                var pattern = cacheKeyPrefix.EndsWith("*") ? cacheKeyPrefix : $"{cacheKeyPrefix}*";
                var keys = await _redisService!.GetAllKeysAsync(pattern);
                if (keys == null || keys.Length == 0)
                    return;

                foreach (var key in keys)
                {
                    await _redisService.DeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"Failed to batch delete cache: {cacheKeyPrefix}, {ex.Message}");
            }
        } 
        #endregion

        #region Advanced Cache Methods

        /// <summary>
        /// Generate cache key hash for FilterDefinition
        /// Render MongoDB Filter as BSON JSON and calculate MD5
        /// </summary>
        /// <param name="filter">MongoDB filter</param>
        /// <returns>MD5 hash string</returns>
        protected string GenerateFilterCacheKey(FilterDefinition<TEntity>? filter)
        {
            if (filter == null)
                return "empty";

            try
            {
                // Get MongoDB serializer
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var documentSerializer = serializerRegistry.GetSerializer<TEntity>();

                // Render Filter as BsonDocument
                var rendered = filter.Render(new RenderArgs<TEntity>
                {
                    DocumentSerializer = documentSerializer,
                    SerializerRegistry = serializerRegistry
                });

                // Convert to JSON string
                var json = rendered.ToJson();

                // Calculate MD5 hash
                byte[] utf8 = Encoding.UTF8.GetBytes(json);
                byte[] hash = MD5.HashData(utf8);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch (Exception ex)
            {
                // If rendering fails, use timestamp as fallback (no caching)
                LogHelper.Warn($"Failed to generate Filter cache key: {ex.Message}");
                return $"filter_{DateTime.UtcNow.Ticks}";
            }
        }

        /// <summary>
        /// Generate cache key with parameters
        /// </summary>
        /// <param name="prefix">Cache key prefix</param>
        /// <param name="filter">MongoDB filter</param>
        /// <param name="suffix">Additional suffix (such as sorting information)</param>
        /// <returns>Complete cache key</returns>
        protected string GenerateCacheKey(string prefix, FilterDefinition<TEntity>? filter, string? suffix = null)
        {
            var filterHash = GenerateFilterCacheKey(filter);
            var parts = new List<string> { prefix, filterHash };

            if (!string.IsNullOrEmpty(suffix))
                parts.Add(suffix);

            return string.Join(":", parts);
        }

        #endregion
    }
}
