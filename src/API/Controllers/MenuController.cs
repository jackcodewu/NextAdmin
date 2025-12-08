using NextAdmin.API.Models;
using NextAdmin.Application.Constants;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Menus;
using NextAdmin.Application.Interfaces;
using NextAdmin.Core.Domain.Entities.Sys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using static NextAdmin.Application.Constants.PermissionsDefine;
using System.Linq;

namespace NextAdmin.API.Controllers
{
    [Route("api/[controller]")]
    public class MenuController : BaseController<Menu, MenuDto, CreateMenuDto, UpdateMenuDto, MenuQueryDto,MenusDto>
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService) : base(menuService)
        {
            _menuService = menuService;
        }

        public override async Task<IActionResult> CreateAsync([FromBody] CreateMenuDto createDto)
        {
            try
            {
                var createdDto = await _menuService.AddAsync(createDto);
                if (createdDto == null)
                    return BadRequest(ApiResponse<object>.ErrorResponse("400", "Operation failed"));

                return Ok(ApiResponse<MenuDto>.SuccessResponse(createdDto, "Added successfully"));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.ErrorResponse("400", "Operation failed, please check if the submitted data is valid or duplicated"));
            }
        }

        /// <summary>
        /// Get current user's menus
        /// </summary>
        [Authorize(Policy = MenuPermissions.View)]
        [HttpGet("user-menus")]
        public async Task<ActionResult<List<MenuDto>>> GetUserMenus()
        {
            // Assuming CurrentUserId is accessible from a base controller or context
            var menus = await _menuService.GetUserMenusAsync(CurrentUserId);
            if (menus?.Any() == true)
                return Ok(ApiResponse<List<MenuDto>>.SuccessResponse(menus));
            else
                return Ok(ApiResponse<List<MenuDto>>.ErrorResponse("400", "No data"));
        }

        [HttpGet("page-list")]
        public override Task<IActionResult> GetListPageAsync([FromQuery] MenuQueryDto queryDto, int pageNumber = 1, int pageSize = int.MaxValue, string sortField = "Id", bool isAsc = false)
        {
            return base.GetListPageAsync(queryDto, pageNumber, pageSize, sortField, isAsc);
        }

        public override async Task<IActionResult> GetOptionsAsync()
        {
            var options = await _menuService.GetOptionsAsync();
            return Ok(ApiResponse<List<MenuOptionDto>>.SuccessResponse(options));
        }

        public override async Task<IActionResult> GetOptionsAsync([FromQuery] MenuQueryDto queryDto, string sortField = "Sort", bool isAsc = false)
        {
            var options = await _menuService.GetOptionsAsync(queryDto, sortField, isAsc);
            return Ok(ApiResponse<List<MenuOptionDto>>.SuccessResponse(options));
        }
    }
} 
