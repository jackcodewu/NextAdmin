using NextAdmin.Application.DTOs.Menus;
using NextAdmin.Application.Interfaces;
using NextAdmin.Application.Services;
using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using MongoDB.Bson;

public interface IMenuService : IAppService<Menu, MenuDto, CreateMenuDto, UpdateMenuDto, MenuQueryDto, MenusDto>
{
    new Task<List<MenuOptionDto>> GetOptionsAsync();
    new Task<List<MenuOptionDto>> GetOptionsAsync(MenuQueryDto queryDto, string sortField = "CreateTime", bool isAsc = false);
    Task<List<MenuDto>> GetUserMenusAsync(string userId);
} 
