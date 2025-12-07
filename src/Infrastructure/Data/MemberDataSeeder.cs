using NextAdmin.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;

namespace NextAdmin.Infrastructure.Data;

/// <summary>
/// 会员相关数据库种子数据
/// </summary>
public static class MemberDataSeeder
{
    /// <summary>
    /// 初始化会员角色
    /// </summary>
    public static async Task SeedMemberRoleAsync(RoleManager<ApplicationRole> roleManager)
    {
        const string roleName = "Member";
        
        // 检查角色是否已存在
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            var memberRole = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpper(),
                Description = "普通会员角色",
                TenantId = ObjectId.Empty,
                TenantName = "System",
                IsSystemRole = false
            };

            var result = await roleManager.CreateAsync(memberRole);
            if (result.Succeeded)
            {
                Console.WriteLine($"会员角色 '{roleName}' 创建成功");
            }
            else
            {
                Console.WriteLine($"会员角色 '{roleName}' 创建失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine($"会员角色 '{roleName}' 已存在");
        }
    }

    /// <summary>
    /// 初始化VIP会员角色
    /// </summary>
    public static async Task SeedVipMemberRoleAsync(RoleManager<ApplicationRole> roleManager)
    {
        const string roleName = "VIPMember";
        
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            var vipRole = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpper(),
                Description = "VIP会员角色",
                TenantId = ObjectId.Empty,
                TenantName = "System",
                IsSystemRole = false
            };

            var result = await roleManager.CreateAsync(vipRole);
            if (result.Succeeded)
            {
                Console.WriteLine($"VIP会员角色 '{roleName}' 创建成功");
            }
            else
            {
                Console.WriteLine($"VIP会员角色 '{roleName}' 创建失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine($"VIP会员角色 '{roleName}' 已存在");
        }
    }

    /// <summary>
    /// 初始化所有会员相关角色
    /// </summary>
    public static async Task SeedAllMemberRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        await SeedMemberRoleAsync(roleManager);
        await SeedVipMemberRoleAsync(roleManager);
    }
}
