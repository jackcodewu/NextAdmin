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
    /// 菜单仓储
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
        /// 创建菜单相关索引
        /// </summary>
        private void CreateIndexes()
        {
            
            // 菜单路径索引（用于路由查询）
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.Path)));
            
            // 菜单标题索引（用于查询）
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.Title)));
            
            // 父级菜单ID索引（用于层级查询）
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.ParentId)));
            
            // 排序索引（用于菜单排序）
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.Sort)));
            
            // 是否隐藏索引（用于过滤）
            Collection.Indexes.CreateOne(new CreateIndexModel<Menu>(
                Builders<Menu>.IndexKeys.Ascending(x => x.IsHide)));
            
        }

        /// <summary>
        /// 递归排序菜单树
        /// </summary>
        /// <param name="menus">菜单列表</param>
        private void SortMenuTree(List<Menu> menus)
        {
            if (menus == null || !menus.Any())
                return;

            // 按 Sort 字段升序排序当前层级
            menus.Sort((a, b) => a.Sort.CompareTo(b.Sort));

            // 递归排序子菜单
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
