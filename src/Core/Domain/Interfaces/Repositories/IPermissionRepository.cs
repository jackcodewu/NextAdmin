using System.Collections.Generic;
using System.Threading.Tasks;
using NextAdmin.Core.Domain.Entities.Sys;
using MongoDB.Bson;

namespace NextAdmin.Core.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Permission repository interface
    /// </summary>
    public interface IPermissionRepository : IBaseRepository<Permission>
    {

    }
} 
