using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using NextAdmin.Application.Constants;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Log;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static NextAdmin.Application.Constants.PermissionsDefine;

namespace NextAdmin.Application.Services
{
    /// <summary>
    /// Data Seeder Service - Initialize system base data
    /// </summary>
    public class DataSeederService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPermissionRepository _permissionRepository;
        private List<Permission> allPermissions = new List<Permission>();
        private bool isUpdate = false;
        private List<Permission> existingPermissions;
        private List<Permission> allExistingPermissions;
        private List<string> existingPermissionCodes;

        public DataSeederService(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IPermissionRepository permissionRepository)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _permissionRepository = permissionRepository;
        }
        public async Task SeedAsync()
        {
            try
            {
                LogHelper.Info("Start seeding data...");
                await SeedPermissionsAsync();
                await SeedRolesAsync();
                await SeedAdminUserAsync();

                LogHelper.Info("Data seeding completed.");
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "An error occurred during data seeding.");
                throw;
            }
        }

        private async Task SeedPermissionsAsync()
        {
            LogHelper.Info("Seeding permissions (tree structure)...");
            List<Permission> updatePermissions = new List<Permission>();
            var permissionsToRemove = new List<Permission>();
            var permissionsToUpdate = new List<Permission>();
            var permissionsToCreate = allPermissions.Select(p => p.DeepClone()).ToList();

            #region MyRegion
            var permissionClasses = typeof(PermissionsDefine)
                .GetNestedTypes()
                .Where(t => t.IsClass);

            foreach (var permissionClasse in permissionClasses)
            {
                var rootAttr = permissionClasse.GetCustomAttribute<PermissionDescriptionAttribute>();
                if (rootAttr == null) continue;

                if (permissionsToCreate?.FirstOrDefault(p => p.Name == rootAttr.Name) != null)
                    continue;

                var parentPermission = new Permission(ObjectId.GenerateNewId())
                {
                    Code = rootAttr.Name,
                    Name = rootAttr.Name,
                    CnName = rootAttr.DisplayName,
                    ParentCode = rootAttr.ParentCode,
                    Sort = rootAttr.Sort,
                };

                permissionsToCreate.Add(parentPermission);


                int index = 0;
                var fields = permissionClasse.GetFields(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                );

                foreach (var field in fields)
                {
                    if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                    {
                        var permissionCode = (string)field.GetRawConstantValue();
                        var permissionName = field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? field.Name;

                        var permission = new Permission(ObjectId.GenerateNewId())
                        {
                            Code = permissionCode,
                            Name = $"{rootAttr.Name}.{field.Name}",
                            CnName = $"{rootAttr.DisplayName}.{permissionName}",
                            ParentCode = parentPermission.Name,
                            Sort = index,
                        };
                        index++;
                        permissionsToCreate.Add(permission);

                        // LogHelper.Info(
                        //     $"Found new permission to create: Code={permissionCode}, Name={permissionName}"
                        // );
                    }
                }
            }
            #endregion

            foreach (var permission in permissionsToCreate)
            {
                if (existingPermissionCodes.Contains(permission.Code))
                {
                    permissionsToRemove.Add(permission);
                    continue;
                }

                var updatePermisson = Permission.GetByParentCode(existingPermissions, permission.ParentCode);
                if (updatePermisson != null)
                {
                    permission.ParentId = updatePermisson.Id;
                    permission.TenantId = updatePermisson.TenantId;
                    permission.TenantName = updatePermisson.TenantName;
                    updatePermisson.Children.Add(permission.DeepClone());
                    updatePermissions.Add(updatePermisson);
                    permissionsToRemove.Add(permission);
                }
            }

            // Remove permissions marked for deletion
            foreach (var permission in permissionsToRemove)
            {
                permissionsToCreate.Remove(permission);
            }

            var rootPermissions = permissionsToCreate.Count == 0 ? new List<Permission>() :
                permissionsToCreate
                .Where(p => p.ParentCode == "")
                .OrderBy(p => p.Sort)
                .ToList();

            for (int i = 0; i < rootPermissions.Count; i++)
            {
                rootPermissions[i] = Permission.BuildPermissionTree(rootPermissions[i], permissionsToCreate, ObjectId.Empty);
            }

            if (updatePermissions.Any())
            {
                isUpdate = true;
                await _permissionRepository.UpdateManyAsync(updatePermissions);
                LogHelper.Info($"{updatePermissions.Count} permissions updated.");
            }

            if (rootPermissions.Any())
            {
                isUpdate = true;
                await _permissionRepository.AddManyAsync(rootPermissions, true);
                LogHelper.Info($"{rootPermissions.Count} new permissions (with groups) created.");
                existingPermissions.AddRange(rootPermissions);
            }
            else
            {
                LogHelper.Info("No new permissions to seed.");
            }

        }

        private async Task SeedRolesAsync()
        {
            LogHelper.Info("--> Seeding roles...");
            string[] roles = { "Admin" };

            foreach (var roleName in roles)
            {
                try
                {
                    LogHelper.Info("--> Checking if role '{RoleName}' exists...", roleName);
                    var roleExists = await _roleManager.RoleExistsAsync(roleName);

                    if (!roleExists)
                    {
                        LogHelper.Info(
                            "--> Role '{RoleName}' does not exist. Creating...",
                            roleName
                        );
                        var applicationRole = new ApplicationRole
                        {
                            Name = roleName,
                            Description = $"{roleName} Role",
                            IsSystemRole = true,
                        };

                        //applicationRole.Menus = _Tenant.Menus.Select(m => m.DeepClone()).ToList();

                        //applicationRole.Permissions = _Tenant
                        //    .Permissions.Select(p => p.DeepClone())
                        //    .ToList();

                        var result = await _roleManager.CreateAsync(applicationRole);
                        if (result.Succeeded)
                        {
                            LogHelper.Info("--> Role '{RoleName}' created successfully.", roleName);
                        }
                        else
                        {
                            var errors = string.Join(
                                ", ",
                                result.Errors.Select(e => e.Description)
                            );
                            LogHelper.Error($"--> Failed to create role '{roleName}': {errors}");
                        }
                    }
                    else
                    {

                        LogHelper.Info("--> Role '{RoleName}' already exists. Skipping.", roleName);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error(
                        ex,
                        "--> An unexpected error occurred while seeding role '{RoleName}'.",
                        roleName
                    );
                }
            }
            LogHelper.Info("--> Role seeding finished.");
        }

        private async Task SeedAdminUserAsync()
        {
            LogHelper.Info("--> Seeding admin user...");
            const string adminEmail = "admin@kb0.com";

            try
            {
                LogHelper.Info("--> Checking if admin user 'admin@kb0.com' exists...");
               var adminUser = await _userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    LogHelper.Info("--> Admin user does not exist. Creating...");
                    await CreateUser();
                }
                else
                {

                    LogHelper.Info("--> Admin user already exists. Checking role assignment...");
                    if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        LogHelper.Info("--> Admin user is not in Admin role. Assigning...");
                        await _userManager.AddToRoleAsync(adminUser, "Admin");
                        LogHelper.Info("--> Assigned Admin role to existing admin user.");
                    }
                    else
                    {
                        LogHelper.Info("--> Admin user is already in Admin role. Skipping.");
                    }
                }

                async Task CreateUser()
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin",
                        Email = adminEmail,
                        FirstName = "System",
                        LastName = "Admin",
                        DisplayName = "System Administrator",
                        Department = "IT",
                        Position = "System Administrator",
                        IsActive = true,
                    };

                    var result = await _userManager.CreateAsync(adminUser, "Admin123!");
                    if (result.Succeeded)
                    {
                        LogHelper.Info(
                            "--> Admin user created successfully. Adding to 'Admin' role..."
                        );
                        await _userManager.AddToRoleAsync(adminUser, "Admin");
                        LogHelper.Info("--> Admin user assigned to Admin role successfully.");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        LogHelper.Error($"--> Failed to create admin user: {errors}");
                    }
                }

                // Create default user zhongkai001
                LogHelper.Info("--> Seeding default user zhongkai001...");
                const string zhongkaiUserName = "zhongkai001";
                const string zhongkaiEmail = "zhongkai001@kb0.com";

                var zhongkaiUser = await _userManager.FindByNameAsync(zhongkaiUserName);
                if (zhongkaiUser == null)
                {
                    LogHelper.Info("--> Default user zhongkai001 does not exist. Creating...");
                    zhongkaiUser = new ApplicationUser
                    {
                        UserName = zhongkaiUserName,
                        Email = zhongkaiEmail,
                        FirstName = "Zhongkai",
                        LastName = "User",
                        DisplayName = "Zhongkai Default User",
                        Department = "Technical Department",
                        Position = "Technician",
                        IsActive = true,
                    };

                    var result = await _userManager.CreateAsync(zhongkaiUser, "121a232");
                    if (result.Succeeded)
                    {
                        LogHelper.Info(
                            "--> Default user zhongkai001 created successfully. Adding to 'Admin' role..."
                        );
                        await _userManager.AddToRoleAsync(zhongkaiUser, "Admin");
                        LogHelper.Info("--> Default user zhongkai001 assigned to Admin role successfully.");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        LogHelper.Error($"--> Failed to create default user zhongkai001: {errors}");
                    }
                }
                else
                {
                    LogHelper.Info("--> Default user zhongkai001 already exists. Checking role assignment...");
                    if (!await _userManager.IsInRoleAsync(zhongkaiUser, "Admin"))
                    {
                        LogHelper.Info("--> Default user zhongkai001 is not in Admin role. Assigning...");
                        await _userManager.AddToRoleAsync(zhongkaiUser, "Admin");
                        LogHelper.Info("--> Assigned Admin role to existing default user zhongkai001.");
                    }
                    else
                    {
                        LogHelper.Info("--> Default user zhongkai001 is already in Admin role. Skipping.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "--> An unexpected error occurred while seeding admin user.");
            }
            LogHelper.Info("--> Admin user seeding finished.");
        }
    }
}
