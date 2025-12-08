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
    /// Permission repository
    /// </summary>
    public class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
    {
        public PermissionRepository(IMongoDatabase database, IRedisService redisService ) : base(database, redisService)
        {
            CreateIndexes();
        }

        /// <summary>
        /// Create permission-related indexes
        /// </summary>
        private void CreateIndexes()
        {
            // Permission code unique index (prevent duplication)
            Collection.Indexes.CreateOne(new CreateIndexModel<Permission>(
                Builders<Permission>.IndexKeys.Ascending(x => x.Code), 
                new CreateIndexOptions { Unique = true }));            
            
            // Permission Chinese name index (for queries)
            Collection.Indexes.CreateOne(new CreateIndexModel<Permission>(
                Builders<Permission>.IndexKeys.Ascending(x => x.CnName)));
            
            // Parent permission ID index (for hierarchical queries)
            Collection.Indexes.CreateOne(new CreateIndexModel<Permission>(
                Builders<Permission>.IndexKeys.Ascending(x => x.ParentId)));
            
            // Is enabled index (for filtering)
            Collection.Indexes.CreateOne(new CreateIndexModel<Permission>(
                Builders<Permission>.IndexKeys.Ascending(x => x.IsEnabled)));
            
        }

    }
} 
