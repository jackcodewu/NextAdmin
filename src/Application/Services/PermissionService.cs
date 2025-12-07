using AutoMapper;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Permissions;
using NextAdmin.Application.Services;
using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Redis;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PermissionService : AppService<Permission, PermissionDto,CreatePermissionDto,UpdatePermissionDto,PermissionQueryDto,PermissionsDto>, IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    public PermissionService(IPermissionRepository permissionRepository, IMapper mapper,
        IRedisService redisService, IHttpContextAccessor httpContextAccessor) :base(permissionRepository,mapper, httpContextAccessor)
    {
        _permissionRepository = permissionRepository;
    }

} 
