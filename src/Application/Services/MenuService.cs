using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoMapper;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.DTOs.Menus;
using NextAdmin.Application.Services;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

public partial class MenuService
    : AppService<Menu, MenuDto, CreateMenuDto, UpdateMenuDto, MenuQueryDto, MenusDto>,
        IMenuService
{
    private readonly IMenuRepository _menuRepository;
    // private readonly ITenantRepository _repository; // 已移除 Tenant 相关功能
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public MenuService(
        IMenuRepository menuRepository,
        // ITenantRepository repository, // 已移除 Tenant 相关功能
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager
    )
            : base(menuRepository, mapper, httpContextAccessor)
    {
        _menuRepository = menuRepository;
        // _repository = repository; // 已移除 Tenant 相关功能
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public override async Task<MenuDto> AddAsync(CreateMenuDto createDto)
    {
        var allMenus = await _menuRepository.GetAllAsync(ObjectId.Empty);

        var menu = _mapper.Map<Menu>(createDto);
        // menu.TenantId = GetTenantId(); // 已移除 Tenant 相关属性
        // menu.TenantName = GetTenantName(); // 已移除 Tenant 相关属性
        if (menu.ParentId == ObjectId.Empty || menu.ParentId==null)
        {
            menu.ParentId = ObjectId.Empty;
            await _menuRepository.AddAsync(menu);
            allMenus.Add(menu);
        }
        else
        {
            await AddMenu(allMenus, menu);
            await _menuRepository.UpdateManyAsync(allMenus);
        }

        // 已移除 Tenant 相关逻辑
        // var Tenants = await _repository.GetAllAsync(GetTenantId());
        // if (Tenants.Any())
        // {
        //     Tenants.ForEach(c =>
        //     {
        //         c.Menus = allMenus.Select(m => m).ToList();
        //     });
        //     await _repository.UpdateManyAsync(Tenants);
        // }

        await DelCache();
        // await DelCache(typeof(Tenant).Name); // 已移除 Tenant

        return _mapper.Map<MenuDto>(menu);
    }
    async Task AddMenu(List<Menu> menus, Menu menu)
    {
        if (menus?.Any() != true)
            return;

        try
        {
            if (menu.ParentId == ObjectId.Empty)
            {
                await repo.AddAsync(menu);
                menus.Add(menu);
                return;
            }
        }
        catch (Exception err)
        {
            LogHelper.Error(err, err.Message);
        }

        var parentMenu = menus.FirstOrDefault(m => m.Id == menu.ParentId);
        if (parentMenu is null)
        {
            for (int i = 0; i < menus.Count; i++)
            {
                if (menus[i].Children?.Any() == true)
                    await AddMenu(menus[i].Children, menu);
            }
        }
        else
        {
            if (parentMenu.Children == null)
                parentMenu.Children = new List<Menu>();

            var m = parentMenu.Children.FirstOrDefault(m => m.Id == menu.Id);
            if (m == null)
                parentMenu.Children.Add(menu);
            else
                m = menu;
        }
    }

    public override async Task<ApiResponse> UpdateAsync(UpdateMenuDto updateDto)
    {
        try
        {
            var allMenus = await _menuRepository.GetAllAsync(ObjectId.Empty);

            var menu = _mapper.Map<Menu>(updateDto);
            // menu.TenantId = CurrentTenantId; // 已移除 Tenant 相关属性
            // menu.TenantName = GetTenantName(); // 已移除 Tenant 相关属性

           await Delete(allMenus, menu);

            if (await Update(allMenus, menu) == false)
                await AddMenu(allMenus, menu);

            await _menuRepository.UpdateManyAsync(allMenus);

            // 已移除 Tenant 相关逻辑
            // var Tenants = await _repository.GetAllAsync(ObjectId.Empty);
            // if (Tenants.Any())
            // {
            //     Tenants.ForEach(async c =>
            //     {
            //         await Update(c.Menus, menu);
            //     });
            //     await _repository.UpdateManyAsync(Tenants);
            // }

            var roles = _roleManager.Roles?.ToList();
            if (roles?.Any() == true)
            {
                foreach (var role in roles)
                {
                   await Update(role.Menus, menu);
                    await _roleManager.UpdateAsync(role);
                }
            }

            async Task Delete(List<Menu> menus, Menu menu)
            {
                if (menus?.Any() != true)
                    return;

                for (int i = 0; i < menus.Count; i++)
                {
                    if (menus[i].Id == menu.Id && menus[i].ParentId!= menu.ParentId)
                    {
                        if (menus[i].ParentId == ObjectId.Empty)
                            await repo.DeleteAsync(menus[i].Id);

                        menus.RemoveAt(i);
                        i--;
                        continue;
                    }

                    //var _menu = menus[i].Children?.FirstOrDefault(p => p.Id == menu.Id && p.ParentId != menu.ParentId);

                    //if (_menu != null)
                    //{
                    //    menus[i].Children.Remove(_menu);
                    //    continue;
                    //}

                    if (menus[i].Children?.Any() == true)
                      await  Delete(menus[i].Children, menu);
                }
            }

            async Task<bool> Update(List<Menu> menus, Menu menu)
            {
                if (menus?.Any() != true)
                    return false;

                for (var i = 0;i< menus.Count;i++)
                {
                    if (menus[i].Id == menu.Id)
                    {
                        menus[i].Id = menu.Id;
                        menus[i].Path = menu.Path;
                        menus[i].Title = menu.Title;
                        menus[i].Name = menu.Name;
                        menus[i].Icon = menu.Icon;
                        menus[i].Component = menu.Component;
                        menus[i].ParentId = menu.ParentId;
                        menus[i].IsHide = menu.IsHide;
                        menus[i].IsKeepAlive = menu.IsKeepAlive;
                        menus[i].IsAffix = menu.IsAffix;
                        menus[i].IsLink = menu.IsLink;
                        menus[i].IsIframe = menu.IsIframe;
                        menus[i].Sort = menu.Sort;
                        menus[i].Redirect = menu.Redirect;
                        return true;
                    }
                    
                    if (menus[i].Children?.Any()==true)
                       await Update(menus[i].Children, menu);
                }

                return false;
            }

            await DelCache();
            return ApiResponse.SuccessResponse("更新成功");
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, ex.Message);
            return ApiResponse.ErrorResponse("500", $"更新失败: {ex.Message}");
        }
    }

    public override async Task<ApiResponse> DeleteAsync(string id)
    {
        try
        {
            ObjectId.TryParse(id, out var menuId);
            if (menuId == ObjectId.Empty)
                return ApiResponse.ErrorResponse("400", "无效的菜单ID");

            var allMenus = await _menuRepository.GetAllAsync(ObjectId.Empty);
            var delMenu = allMenus.FirstOrDefault(c => c.Id == menuId);
            if (delMenu is not null && delMenu.ParentId == ObjectId.Empty)
            {
                await _menuRepository.DeleteAsync(menuId);
            }
            else
            {
                Delete(allMenus, menuId);
                await _menuRepository.UpdateManyAsync(allMenus);
            }

            // 已移除 Tenant 相关逻辑
            // var Tenants = await _repository.GetAllAsync(ObjectId.Empty);
            // if (Tenants.Any())
            // {
            //     Tenants.ForEach(c =>
            //     {
            //         c.Menus = allMenus.Select(m => m).ToList();
            //     });
            //     await _repository.UpdateManyAsync(Tenants);
            // }

            var roles = _roleManager.Roles?.ToList();
            if (roles?.Any() == true)
            {
                foreach (var role in roles)
                {
                    Delete(role.Menus, menuId);
                    await _roleManager.UpdateAsync(role);
                }
            }

            void Delete(List<Menu> menus, ObjectId menuId)
            {
                if (menus?.Any() != true)
                    return;

                foreach (var _menu in menus)
                {
                    if (_menu.Id == menuId)
                    {
                        menus.Remove(_menu);
                        return;
                    }

                    if (_menu.Children?.Any()==true)
                        Delete(_menu.Children, menuId);
                }
            }

            await DelCache();
            // await DelCache(typeof(Tenant).Name); // 已移除 Tenant

            return ApiResponse.SuccessResponse("删除成功");
        }
        catch (Exception ex)
        {
            NextAdmin.Log.LogHelper.Error(ex, ex.Message);
            return ApiResponse.ErrorResponse("500", $"删除失败: {ex.Message}");
        }
    }

    public async Task<List<MenuDto>> GetUserMenusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return new List<MenuDto>();

        var roles = await _userManager.GetRolesAsync(user);
        var roleMenus = new Dictionary<string, List<Menu>>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role?.Menus?.Any() == true)
            {
                roleMenus.Add(roleName, role.Menus);
            }
        }

        return _mapper.Map<List<MenuDto>>(
            roleMenus
                .OrderByDescending(rm => rm.Value.Count)
                .FirstOrDefault()
                .Value.OrderBy(m => m.Sort)
        );
    }

    public override async Task<List<MenusDto>> GetAllAsync()
    {
        var treeMenus = await base.GetAllAsync();
        var flatList = new List<MenusDto>();

        void Flatten(IEnumerable<MenusDto> menus)
        {
            foreach (var menu in menus)
            {
                flatList.Add(menu);
                if (menu.Children != null && menu.Children?.Any()==true)
                {
                    Flatten(menu.Children);
                    menu.Children.Clear();
                }
            }
        }

        Flatten(treeMenus);
        return flatList;
    }

    public override async Task<MenuDto> GetAsync(string id)
    {
        var treeMenus = await base.GetAllAsync();
        var result = SearchMenu(treeMenus);
        return _mapper.Map<MenuDto>(result);

        MenusDto? SearchMenu(List<MenusDto> menus)
        {
            foreach (var menu in menus)
            {
                if (menu.Id == id)
                {
                    return menu;
                }
                if (menu.Children != null && menu.Children?.Any()==true)
                {
                    var result = SearchMenu(menu.Children);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }

    public new async Task<List<MenuOptionDto>> GetOptionsAsync()
    {
        var treeMenus = await base.GetAllAsync();
        return _mapper.Map<List<MenuOptionDto>>(treeMenus);
    }

    public new async Task<List<MenuOptionDto>> GetOptionsAsync(MenuQueryDto queryDto, string sortField = "Sort", bool isAsc = true)
    {
        var treeMenus = await base.GetsAsync(queryDto, "Sort", isAsc);

        return _mapper.Map<List<MenuOptionDto>>(treeMenus);
    }

    public override Task<PagedResultDto<MenusDto>> GetListPageAsync(MenuQueryDto queryDto, int pageNumber, int pageSize, string sortField = "Sort", bool isAsc = true)
    {
        var result = base.GetListPageAsync(queryDto, pageNumber, pageSize, "Sort", true);

        return result;
    }

    /// <summary>
    /// 递归过滤菜单树
    /// </summary>
    private List<Menu> FilterMenuTree(List<Menu> menus)
    {
        var filteredList = new List<Menu>();

        foreach (var menu in menus)
        {
            if (!menu.IsEnabled)
                continue;

            filteredList.Add(menu);

            // If the menu has children, recursively filter them.
            if (menu.Children != null && menu.Children?.Any()==true)
            {
                menu.Children = FilterMenuTree(menu.Children);
            }
        }
        return filteredList;
    }
}
