using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Redis;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NextAdmin.Infrastructure.Repositories
{
    /// <summary>
    /// Menu repository
    /// </summary>
    public class MenuRepository : BaseRepository<Menu>, IMenuRepository
    {
        public MenuRepository(IMongoDatabase database, IRedisService redisService) : base(database, redisService)
        {
            CreateIndexes();
        }

        public override async Task<bool> DeleteAsync(ObjectId id)
        {
            var result = await Collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }

        /// <summary>
        /// Create menu-related indexes
        /// </summary>
        private void CreateIndexes()
        {
            
            // Menu path index (for routing queries)
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.Path)));
            
            // Menu title index (for queries)
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.Title)));
            
            // Parent menu ID index (for hierarchical queries)
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.ParentId)));
            
            // Sort index (for menu sorting)
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.Sort)));
            
            // Is hidden index (for filtering)
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.IsHide)));
            
        }

        /// <summary>
        /// Recursively sort menu tree
        /// </summary>
        /// <param name="menus">Menu list</param>
        private void SortMenuTree(List<Menu> menus)
        {
            if (menus == null || !menus.Any())
                return;

            // Sort current level in ascending order by Sort field
            menus.Sort((a, b) => a.Sort.CompareTo(b.Sort));

            // Recursively sort child menus
            foreach (var menu in menus)
            {
                if (menu.Children != null && menu.Children.Any())
                {
                    SortMenuTree(menu.Children);
                }
            }
        }

        public override async Task<Menu?> GetOneAsync(FilterDefinition<Menu> filter, ObjectId TenantId)
        {
            var menu = await base.GetOneAsync(filter, TenantId);
            if (menu != null && menu.Children != null && menu.Children.Any())
            {
                SortMenuTree(menu.Children);
            }
            return menu;
        }

        public override async Task<Menu> GetByIdAsync(ObjectId id)
        {
            var menu = await base.GetByIdAsync(id);
            if (menu != null && menu.Children != null && menu.Children.Any())
            {
                SortMenuTree(menu.Children);
            }
            return menu;
        }

        public override async Task<List<Menu>> GetAllAsync(ObjectId TenantId)
        {
            var menus = await base.GetAllAsync(TenantId);
            SortMenuTree(menus);
            return menus;
        }

        public override async Task<List<Menu>> GetAsync(FilterDefinition<Menu> predicate, ObjectId TenantId)
        {
            var menus = await base.GetAsync(predicate, TenantId);
            SortMenuTree(menus);
            return menus;
        }

        public override async Task<(List<Menu> Items, long Total)> GetListPageAsync(int pageNumber, int pageSize, FilterDefinition<Menu>? expression, ObjectId TenantId, string sortField = "Sort", bool isAsc = true)
        {
            var result = await base.GetListPageAsync(pageNumber, pageSize, expression, TenantId, sortField, isAsc);
            return result;
        }

        public override Task<(List<TProjection> Items, long Total)> GetListPageAsync<TProjection>(int pageNumber, int pageSize, FilterDefinition<Menu>? expression, ObjectId TenantId, ProjectionDefinition<Menu, TProjection>? projection = null, string sortField = "Sort", bool isAsc = true)
        {
            return base.GetListPageAsync(pageNumber, pageSize, expression, TenantId, projection, sortField, isAsc);
        }

    }
} 
