using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Permissions;
using NextAdmin.Application.Interfaces;
using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPermissionService : IAppService<Permission, PermissionDto, CreatePermissionDto, UpdatePermissionDto, PermissionQueryDto, PermissionsDto>
{
} 
