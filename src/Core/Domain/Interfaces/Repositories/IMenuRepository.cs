using System.Collections.Generic;
using System.Threading.Tasks;
using NextAdmin.Core.Domain.Entities.Sys;
using MongoDB.Bson;

namespace NextAdmin.Core.Domain.Interfaces.Repositories
{
    /// <summary>
    /// 菜单仓储接口
    /// </summary>
    public interface IMenuRepository : IBaseRepository<Menu>
    {

    }
} 
