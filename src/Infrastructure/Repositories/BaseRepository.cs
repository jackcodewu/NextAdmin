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
    /// MongoDB仓储基类
    /// 提供基本的CRUD操作和集合访问
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public partial class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : AggregateRoot
    {
        #region 局部更新配置
        /// <summary>
        /// 属性反射缓存（静态，所有实例共享）
        /// </summary>
        private static readonly PropertyInfo[] CachedProperties;
        private static readonly HashSet<string> SkipPropertyNames = new() { "Id", "UpdateTime", "VIPCommands,CreatedById", "CreatedByName", "CreateTime" };

        /// <summary>
        /// 检查属性是否有 BsonIgnore 特性
        /// </summary>
        private static bool HasBsonIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<MongoDB.Bson.Serialization.Attributes.BsonIgnoreAttribute>() != null;
        }

        /// <summary>
        /// 判断类型是否为 MongoDB 不支持序列化的类型
        /// </summary>
        private static bool IsUnsupportedType(Type type)
        {
            // 跳过 ConcurrentQueue 等并发集合类型
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
        // 3. 更新
        /// <summary>
        /// 判断属性值是否有效（非默认值）
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
                System.Collections.ICollection collection => collection.Count >= 0, // 集合类型:只有非空集合才算有值
                bool _ => true, // bool 类型: false 和 true 都是有效值，不应跳过
                _ when value.GetType().IsValueType => !value.Equals(Activator.CreateInstance(value.GetType())),
                _ => true // 其他引用类型且不为 null
            };
        }

        /// <summary>
        /// 构建更新定义（只包含有值的属性）
        /// </summary>
        private static List<UpdateDefinition<TEntity>> BuildUpdateDefinitions(TEntity entity)
        {
            var updateDefinitionList = new List<UpdateDefinition<TEntity>>
            {
                // 始终更新 UpdateTime
                Builders<TEntity>.Update.Set(x => x.UpdateTime, DateTime.UtcNow)
            };

            // 使用缓存的属性信息
            foreach (var property in CachedProperties)
            {
                var value = property.GetValue(entity);

                // 只有有值时才添加到更新定义
                if (HasValue(value))
                {
                    var updateDef = Builders<TEntity>.Update.Set(property.Name, value);
                    updateDefinitionList.Add(updateDef);
                }
            }

            return updateDefinitionList;
        } 
        #endregion

        #region 字段
        /// <summary>
        /// MongoDB数据库实例
        /// </summary>
        protected readonly IMongoDatabase Database;
        private readonly string collectionName;
        protected readonly IRedisService? _redisService;
        protected readonly string _key;

        private bool _supportsCaching = false;
        /// <summary>
        /// 是否支持缓存
        /// </summary>
        public virtual bool SupportsCaching
        {
            get => _supportsCaching;
            set { _supportsCaching = value; }
        }

        /// <summary>
        /// 缓存过期时间
        /// </summary>
        public virtual TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// 实体集合
        /// </summary>
        protected readonly IMongoCollection<TEntity> Collection; 
        #endregion

        #region 构造函数 
        /// <summary>
        /// 静态构造函数，初始化属性缓存
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
        /// 构造函数
        /// </summary>
        /// <param name="database">MongoDB数据库实例</param>
        /// <param name="collectionName">集合名称</param>
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
        
        #region 创建索引

        protected void CreateNameIndex(bool isUnique)
        {
            //Name唯一索引
            var nameIndex = Builders<TEntity>.IndexKeys.Ascending(x => x.Name);
            Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(nameIndex, new CreateIndexOptions { Unique = isUnique }));
        }

        private void CreateIndexes()
        {
            try
            {

                // CreateTime降序索引
                var createTimeIndex = Builders<TEntity>.IndexKeys.Descending(x => x.CreateTime);
                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(createTimeIndex));

                // CreateTime + _id 复合索引（降序）- 用于 seek 分页
                var createTimeIdDescIndex = Builders<TEntity>.IndexKeys
                    .Descending(x => x.CreateTime)
                    .Descending(x => x.Id);
                Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(createTimeIdDescIndex,
                    new CreateIndexOptions { Name = "CreateTime_Id_desc" }));

                // CreateTime + _id 复合索引（升序）- 用于 seek 分页
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
                // 索引已存在，忽略错误
                LogHelper.Info($"索引已存在，跳过创建: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"创建索引时发生错误: {ex.Message}");
                throw;
            }
        } 
        #endregion

        #region 新增功能

        // 2. 添加
        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="TEntity">实体</param>
        /// <returns>添加的实体</returns>
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
        /// 批量添加实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体列表</param>
        /// <returns>添加成功的数量</returns>
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

        #region 更新功能

        /// <summary>
        /// 更新实体（只更新非空属性）
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="TEntity">实体</param>
        /// <param name="hasEmpty">是否允许空的 TenantId</param>
        /// <returns>更新后的实体</returns>
        public virtual async Task<TEntity> UpdateAsync(ObjectId id, TEntity TEntity, bool hasEmpty = false)
        {
            try
            {

                // 构建更新定义
                var updateDefinitionList = BuildUpdateDefinitions(TEntity);

                // 如果没有需要更新的字段，返回原实体
                if (updateDefinitionList.Count == 0)
                    return await GetByIdAsync(id);

                // 合并所有更新定义
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
        /// 更新实体（只更新非空属性）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="TEntity">实体</param>
        /// <param name="hasEmpty">是否允许空的 TenantId</param>
        /// <returns>是否成功</returns>
        public virtual async Task<bool> UpdateAsync(TEntity TEntity, bool hasEmpty = false)
        {
            try
            {

                // 构建更新定义
                var updateDefinitionList = BuildUpdateDefinitions(TEntity);

                // 如果没有需要更新的字段，直接返回
                if (updateDefinitionList.Count == 0)
                    return true;

                // 合并所有更新定义
                var updateDefinition = Builders<TEntity>.Update.Combine(updateDefinitionList);

                var collection = Database.GetCollection<TEntity>(collectionName);
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, TEntity.Id);
                var result = await collection.UpdateOneAsync(filter, updateDefinition);

                if (SupportsCaching && result.ModifiedCount > 0)
                {
                    await DelCache();
                    // 重新获取完整实体并缓存
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
        /// 批量更新实体（只更新非空属性）
        /// </summary>
        /// <param name="entities">实体列表</param>
        /// <param name="hasEmpty">是否允许空的 TenantId</param>
        public virtual async Task UpdateManyAsync(List<TEntity> entities, bool hasEmpty = false)
        {
            try
            {
                if (entities == null || !entities.Any())
                    return;

                var bulkOps = new List<WriteModel<TEntity>>();
                foreach (var entity in entities)
                {
                    // 使用优化后的方法构建更新定义
                    var updateDefinitionList = BuildUpdateDefinitions(entity);

                    // 如果有需要更新的字段，添加到批量操作
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


        #region 查询功能
        /// <summary>
        /// 生成带 TenantId 的基础过滤器
        /// </summary>
        protected FilterDefinition<TEntity> BuildTenantFilter(ObjectId TenantId)
        {
            var filter = Builders<TEntity>.Filter.Eq(x => x.IsDeleted, false);

            return filter;
        }

        // 1. 获取
        /// <summary>
        /// 根据ID和公司ID获取实体
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
        /// 获取单个实体（带公司ID）
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
        /// 获取所有实体（带公司ID）
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllAsync(ObjectId TenantId)
        {
            // 生成缓存键（无参数方法使用固定键）
            var cacheKey = $"{_key}:all";

            // 尝试从缓存获取
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
        /// 获取所有实体（带公司ID和排序）
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllAsync(ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false)
        {
            // 生成缓存键（无参数方法使用固定键）
            var cachekey = $"{_key}:all:e";

            // 尝试从缓存获取
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
        /// 条件查询（带公司ID）
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
        /// 条件查询（带公司ID和排序）
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
        /// 条件查询并直接投影为指定类型（带公司ID和排序）
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
        /// 条件查询（使用MongoDB原生过滤器，带公司ID和排序）
        /// </summary>
        public virtual async Task<List<TEntity>> GetAsync(FilterDefinition<TEntity> filter, string sortField = "CreateTime", bool isAsc = false)
        {

            var baseFilter = BuildTenantFilter(ObjectId.Empty);
            if (filter != null)
                baseFilter &= filter;


            // 生成缓存键（无参数方法使用固定键）
            var cacheKey = GenerateCacheKey($"{_key}:sort", baseFilter);

            // 尝试从缓存获取
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
        /// 获取分页数据（使用 Seek 游标分页，支持 CreateTime + _id 排序）
        /// 游标信息保存到 Redis，支持随机页跳转
        /// </summary>
        public virtual async Task<(List<TEntity> Items, long Total)> GetListPageAsync(
            int pageNumber, int pageSize, FilterDefinition<TEntity>? expression, ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var filter = BuildTenantFilter(TenantId);
            if (expression != null)
                filter &= expression;

            // 生成游标缓存键（包含筛选条件和排序）
            var filterHash = GenerateFilterCacheKey(filter);
            var sortDirection = isAsc ? "asc" : "desc";
            var cursorCacheKeyPrefix = $"{_key}:cursor:{filterHash}:{sortField}:{sortDirection}";
            var dataCacheKey = $"{cursorCacheKeyPrefix}:page:{pageNumber}:{pageSize}";

            // 尝试从缓存获取数据
            if (SupportsCaching)
            {
                var cached = await GetCacheObjectAsync<(List<TEntity> Items, long Total)>(dataCacheKey);
                if (cached.Items != null && cached.Items.Count > 0)
                    return cached;
            }

            // 获取总数（并行）
            var totalTask = CountAsync(filter, TenantId);

            // 获取或计算游标位置
            string cursorKey = $"{cursorCacheKeyPrefix}:page:{pageNumber - 1}";
            SeekCursor? cursor = null;

            if (pageNumber > 1 && SupportsCaching)
            {
                cursor = await GetCacheObjectAsync<SeekCursor>(cursorKey);
            }

            // 构建查询
            var seekFilter = filter;
            if (cursor != null)
            {
                // 使用游标继续查询
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

            // 构建排序
            var sortDefinition = isAsc
                ? Builders<TEntity>.Sort.Ascending(sortField).Ascending(x => x.Id)
                : Builders<TEntity>.Sort.Descending(sortField).Descending(x => x.Id);

            // 执行查询
            var query = Collection.Find(seekFilter).Sort(sortDefinition).Limit(pageSize);
            var items = await query.ToListAsync();
            var total = await totalTask;

            // 保存当前页的游标到 Redis（用于下一页）
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

                // 缓存数据结果
                await SetCacheObjectAsync(dataCacheKey, (items, total), TimeSpan.FromMinutes(10));
            }

            return (items, total);
        }

        /// <summary>
        /// 获取分页数据（使用 Seek 游标分页 + MongoDB 投影，返回轻量 DTO）
        /// 游标信息保存到 Redis，支持随机页跳转
        /// 适用于列表查询，避免加载大字段（如 Commands）
        /// </summary>
        /// <typeparam name="TProjection">投影类型（轻量 DTO）</typeparam>
        // ===================== BaseRepository<TEntity> 类内 =====================

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

            // 生成缓存键（包含筛选条件/排序/投影）——与原实现保持一致
            var filterHash = GenerateFilterCacheKey(filter);
            var sortDirection = isAsc ? "asc" : "desc";
            var projectionHash = projection?.ToString()?.GetHashCode().ToString() ?? "full";
            var cursorCacheKeyPrefix = $"{_key}:cursor:{filterHash}:{sortField}:{sortDirection}:{projectionHash}";
            var dataCacheKey = $"{cursorCacheKeyPrefix}:page:{pageNumber}:{pageSize}";

            // 先尝试命中整页缓存
            if (SupportsCaching)
            {
                var cached = await GetCacheObjectAsync<(List<TProjection> Items, long Total)>(dataCacheKey);
                if (cached.Items != null && cached.Items.Count > 0)
                    return cached;
            }

            // 并行计算总数
            var totalTask = CountAsync(filter, TenantId);

            // 读取上一页游标（用于 Seek）
            string cursorKey = $"{cursorCacheKeyPrefix}:page:{pageNumber - 1}";
            SeekCursor? cursor = null;

            if (pageNumber > 1 && SupportsCaching)
            {
                cursor = await GetCacheObjectAsync<SeekCursor>(cursorKey);
            }

            // ====== ✅ 兜底：缺上一页游标时，用 Skip/Limit 直接拿这一页 ======
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

                // 写回“本页”的游标 & 缓存本页数据（便于下一页/刷新）
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
            // ====== ✅ 兜底结束，正常 Seek 路径继续 ======

            // 构建 Seek 过滤
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

            // 排序：主排序 + Id 作为稳定性二级排序
            var sortDefinition = isAsc
                ? Builders<TEntity>.Sort.Ascending(sortField).Ascending(x => x.Id)
                : Builders<TEntity>.Sort.Descending(sortField).Descending(x => x.Id);

            // 执行查询（可带投影）
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

            // 保存本页的游标与数据
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
        /// 从投影对象中提取 Id
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
        /// 从投影对象中提取排序字段值
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

                // 转换为 BsonValue
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
        /// 游标信息类
        /// </summary>
        private class SeekCursor
        {
            public ObjectId Id { get; set; }
            public BsonValue SortValue { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }

        /// <summary>
        /// 通过反射获取排序字段的值
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

                // 转换为 BsonValue
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

                // 默认转换
                return BsonValue.Create(value);
            }
            catch
            {
                return BsonNull.Value;
            }
        }

        // 5. 统计
        /// <summary>
        /// 获取实体数量（带公司ID）
        /// </summary>
        public async Task<long> CountAsync(FilterDefinition<TEntity> filter, ObjectId TenantId)
        {

            var baseFilter = BuildTenantFilter(TenantId);
            if (filter != null)
                baseFilter &= filter;

            var cacheKey = GenerateCacheKey($"{_key}:count", baseFilter);

            // 尝试从缓存获取
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

            // 尝试从缓存获取
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

        #region 删除功能

        // 4. 删除
        /// <summary>
        /// 删除实体（带公司ID）
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

        #region 缓存相关
        // 其他辅助方法
        /// <summary>
        /// 获取集合名称
        /// </summary>
        /// <returns>集合名称</returns>
        public string GetCollectionName()
        {
            return collectionName;
        }

        /// <summary>
        /// 获取缓存值
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
                LogHelper.Warn($"获取缓存失败: {cacheKey}, {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 设置缓存值
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
                LogHelper.Warn($"设置缓存失败: {cacheKey}, {ex.Message}");
            }
        }

        /// <summary>
        /// 删除缓存
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
        /// 删除指定缓存
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
                LogHelper.Warn($"删除缓存失败: {cacheKey}, {ex.Message}");
            }
        }

        /// <summary>
        /// 根据前缀批量删除缓存
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
                LogHelper.Warn($"批量删除缓存失败: {cacheKeyPrefix}, {ex.Message}");
            }
        } 
        #endregion

        #region 高级缓存方法

        /// <summary>
        /// 生成 FilterDefinition 的缓存键哈希
        /// 将 MongoDB Filter 渲染为 BSON JSON 并计算 MD5
        /// </summary>
        /// <param name="filter">MongoDB 过滤器</param>
        /// <returns>MD5 哈希字符串</returns>
        protected string GenerateFilterCacheKey(FilterDefinition<TEntity>? filter)
        {
            if (filter == null)
                return "empty";

            try
            {
                // 获取 MongoDB 序列化器
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var documentSerializer = serializerRegistry.GetSerializer<TEntity>();

                // 将 Filter 渲染为 BsonDocument
                var rendered = filter.Render(new RenderArgs<TEntity>
                {
                    DocumentSerializer = documentSerializer,
                    SerializerRegistry = serializerRegistry
                });

                // 转换为 JSON 字符串
                var json = rendered.ToJson();

                // 计算 MD5 哈希
                byte[] utf8 = Encoding.UTF8.GetBytes(json);
                byte[] hash = MD5.HashData(utf8);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch (Exception ex)
            {
                // 如果渲染失败，使用时间戳作为后备（不使用缓存）
                LogHelper.Warn($"生成Filter缓存键失败: {ex.Message}");
                return $"filter_{DateTime.UtcNow.Ticks}";
            }
        }

        /// <summary>
        /// 生成带参数的缓存键
        /// </summary>
        /// <param name="prefix">缓存键前缀</param>
        /// <param name="filter">MongoDB 过滤器</param>
        /// <param name="suffix">额外后缀（如排序信息）</param>
        /// <returns>完整的缓存键</returns>
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
