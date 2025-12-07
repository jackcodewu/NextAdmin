using System.Collections.Generic;
using System.Threading.Tasks;
using NextAdmin.Core.Domain.Entities.Sys;
using MongoDB.Bson;

namespace NextAdmin.Core.Domain.Interfaces.Repositories
{
    /// <summary>
    /// 权限仓储接口
    /// </summary>
    public interface IPermissionRepository : IBaseRepository<Permission>
    {

    }
} 
