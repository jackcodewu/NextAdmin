using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Redis;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextAdmin.Infrastructure.Repositories
{
    /// <summary>
    /// 权限仓储
    /// </summary>
    public class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
    {
        public PermissionRepository(IMongoDatabase database, IRedisService redisService ) : base(database, redisService)
        {
            CreateIndexes();
        }

        /// <summary>
        /// 创建权限相关索引
        /// </summary>
        private void CreateIndexes()
        {
            // 权限代码唯一索引（防止重复）
            Collection.Indexes.CreateOne(new CreateIndexModel<Permission>(
                Builders<Permission>.IndexKeys.Ascending(x => x.Code), 
                new CreateIndexOptions { Unique = true }));            
            
            // 权限中文名称索引（用于查询）
            Collection.Indexes.CreateOne(new CreateIndexModel<Permission>(
                Builders<Permission>.IndexKeys.Ascending(x => x.CnName)));
            
            // 父级权限ID索引（用于层级查询）
            Collection.Indexes.CreateOne(new CreateIndexModel<Permission>(
                Builders<Permission>.IndexKeys.Ascending(x => x.ParentId)));
            
            // 是否启用索引（用于过滤）
            Collection.Indexes.CreateOne(new CreateIndexModel<Permission>(
                Builders<Permission>.IndexKeys.Ascending(x => x.IsEnabled)));
            
        }

    }
} 
