using NextAdmin.API.Models;
using NextAdmin.Application.Constants;
using NextAdmin.Application.DTOs.Permissions;
using NextAdmin.Core.Domain.Entities.Sys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NextAdmin.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionController : BaseController<Permission,PermissionDto, CreatePermissionDto,UpdatePermissionDto,PermissionQueryDto, PermissionsDto>
    {
        private readonly IPermissionService _permissionService;

        public PermissionController(IPermissionService permissionService):base(permissionService) 
        {
            _permissionService = permissionService;
        }

    }
} 
