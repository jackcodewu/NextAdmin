using NextAdmin.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;

namespace NextAdmin.Infrastructure.Data;

/// <summary>
/// Member-related database seed data
/// </summary>
public static class MemberDataSeeder
{
    /// <summary>
    /// Initialize member role
    /// </summary>
    public static async Task SeedMemberRoleAsync(RoleManager<ApplicationRole> roleManager)
    {
        const string roleName = "Member";
        
        // Check if the role already exists
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            var memberRole = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpper(),
                Description = "Regular member role",
                TenantId = ObjectId.Empty,
                TenantName = "System",
                IsSystemRole = false
            };

            var result = await roleManager.CreateAsync(memberRole);
            if (result.Succeeded)
            {
                Console.WriteLine($"Member role '{roleName}' created successfully");
            }
            else
            {
                Console.WriteLine($"Failed to create member role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine($"Member role '{roleName}' already exists");
        }
    }

    /// <summary>
    /// Initialize VIP member role
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
                Description = "VIP member role",
                TenantId = ObjectId.Empty,
                TenantName = "System",
                IsSystemRole = false
            };

            var result = await roleManager.CreateAsync(vipRole);
            if (result.Succeeded)
            {
                Console.WriteLine($"VIP member role '{roleName}' created successfully");
            }
            else
            {
                Console.WriteLine($"Failed to create VIP member role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine($"VIP member role '{roleName}' already exists");
        }
    }

    /// <summary>
    /// Initialize all member-related roles
    /// </summary>
    public static async Task SeedAllMemberRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        await SeedMemberRoleAsync(roleManager);
        await SeedVipMemberRoleAsync(roleManager);
    }
}
