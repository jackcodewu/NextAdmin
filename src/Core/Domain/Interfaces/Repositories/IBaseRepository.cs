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
    /// MongoDB通用仓储接口
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IBaseRepository<TEntity> where TEntity : AggregateRoot
    {
        /// <summary>
        /// 是否支持缓存
        /// </summary>
        bool SupportsCaching { get; set; }

        /// <summary>
        /// 缓存过期时间
        /// </summary>
        TimeSpan CacheExpiry { get; set; }

        /// <summary>
        /// 获取缓存对象
        /// </summary>
        Task<T?> GetCacheObjectAsync<T>(string cacheKey);

        /// <summary>
        /// 设置缓存对象
        /// </summary>
        Task SetCacheObjectAsync<T>(string cacheKey, T value, TimeSpan? expiry = null);

        /// <summary>
        /// 删除指定缓存
        /// </summary>
        Task RemoveCacheObjectAsync(string cacheKey);

        /// <summary>
        /// 根据前缀删除缓存
        /// </summary>
        Task RemoveCacheByPrefixAsync(string cacheKeyPrefix);

        // 1. 获取
        /// <summary>
        /// 根据ID和公司ID获取实体
        /// </summary>
        Task<TEntity> GetByIdAsync(ObjectId id);

        /// <summary>
        /// 获取所有实体
        /// </summary>
        Task<List<TEntity>> GetAllAsync(ObjectId TenantId);

        /// <summary>
        /// 获取所有实体（带排序）
        /// </summary>
        Task<List<TEntity>> GetAllAsync(ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false);

        /// <summary>
        /// 条件查询（带公司ID）
        /// </summary>
        Task<List<TEntity>> GetAsync(FilterDefinition<TEntity> filter, ObjectId TenantId);

        /// <summary>
        /// 条件查询（带公司ID和排序）
        /// </summary>
        Task<List<TEntity>> GetAsync(FilterDefinition<TEntity> filter, ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false);

    /// <summary>
    /// 条件查询并直接投影为指定类型（带公司ID/排序）
    /// 如果提供 projection 则使用 MongoDB 投影，否则尝试将结果作为 TProjection 返回
    /// </summary>
    Task<List<TProjection>> GetAsync<TProjection>(FilterDefinition<TEntity> filter, ObjectId TenantId, ProjectionDefinition<TEntity, TProjection>? projection = null, string sortField = "CreateTime", bool isAsc = false);

        /// <summary>
        /// 获取分页数据（带公司ID）
        /// </summary>
        Task<(List<TEntity> Items, long Total)> GetListPageAsync(int pageNumber, int pageSize, FilterDefinition<TEntity>? expression, ObjectId TenantId, string sortField = "CreateTime", bool isAsc = false);

        // 2. 添加
        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="TEntity">实体</param>
        /// <returns>添加的实体</returns>
        Task<TEntity> AddAsync(TEntity TEntity, bool hasEmpty = false);

        /// <summary>
        /// 批量添加实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体列表</param>
        /// <returns>添加成功的数量</returns>
        Task<int> AddManyAsync(List<TEntity> entities, bool hasEmpty = false);

        // 3. 更新
        /// <summary>
        /// 更新实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="TEntity">实体</param>
        /// <returns>更新后的实体</returns>
        Task<TEntity> UpdateAsync(ObjectId id, TEntity TEntity, bool hasEmpty = false);

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="TEntity">实体</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateAsync(TEntity TEntity, bool hasEmpty = false);

        /// <summary>
        /// 批量更新实体
        /// </summary>
        /// <param name="entities">实体列表</param>
        Task UpdateManyAsync(List<TEntity> entities, bool hasEmpty = false);

        // 4. 删除
        /// <summary>
        /// 删除实体（带公司ID）
        /// </summary>
        Task<bool> DeleteAsync(ObjectId id);

        // 5. 统计
        /// <summary>
        /// 获取实体数量（带公司ID）
        /// </summary>
        Task<long> CountAsync(FilterDefinition<TEntity> filter, ObjectId TenantId);

        /// <summary>
        /// 获取单个实体（带公司ID）
        /// </summary>
        Task<TEntity?> GetOneAsync(FilterDefinition<TEntity> filter, ObjectId TenantId);

        // 其他辅助方法
        /// <summary>
        /// 获取集合名称
        /// </summary>
        /// <returns>集合名称</returns>
        string GetCollectionName();
        Task<long> CountAsync(ObjectId TenantId);
        Task<(List<TProjection> Items, long Total)> GetListPageAsync<TProjection>(int pageNumber, int pageSize, FilterDefinition<TEntity>? expression, ObjectId TenantId, ProjectionDefinition<TEntity, TProjection>? projection = null, string sortField = "CreateTime", bool isAsc = false);
    }
}



