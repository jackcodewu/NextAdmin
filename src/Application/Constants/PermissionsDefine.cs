using System.ComponentModel;

namespace NextAdmin.Application.Constants
{
    /// <summary>
    /// 权限定义
    /// 示例权限定义，可根据实际业务需求扩展
    /// </summary>
    public static class PermissionsDefine
    {
        public const string GroupName = "NextAdmin";

        /// <summary>
        /// 租户管理
        /// </summary>
        [PermissionDescription("", "TenantManage", "租户管理", 0)]
        public static class TenantManagePermissions
        {
            [Description("查看")]
            public const string View = "TenantManage.View";
        }

        /// <summary>
        /// 租户
        /// </summary>
        [PermissionDescription("TenantManage", "Tenant", "租户", 0)]
        public static class TenantPermissions
        {
            [Description("查看")]
            public const string View = "Tenant.View";
            [Description("创建")]
            public const string Create = "Tenant.Create";
            [Description("编辑")]
            public const string Edit = "Tenant.Edit";
            [Description("删除")]
            public const string Delete = "Tenant.Delete";
        }

        /// <summary>
        /// 权限管理
        /// </summary>
        [PermissionDescription("TenantManage", "Permission", "权限管理", 1)]
        public static class PermissionPermissions
        {
            [Description("查看")]
            public const string View = "Permission.View";
            [Description("创建")]
            public const string Create = "Permission.Create";
            [Description("编辑")]
            public const string Edit = "Permission.Edit";
            [Description("删除")]
            public const string Delete = "Permission.Delete";
        }

        /// <summary>
        /// 角色管理
        /// </summary>
        [PermissionDescription("TenantManage", "Role", "角色管理", 2)]
        public static class RolePermissions
        {
            [Description("查看")]
            public const string View = "Role.View";
            [Description("创建")]
            public const string Create = "Role.Create";
            [Description("编辑")]
            public const string Edit = "Role.Edit";
            [Description("删除")]
            public const string Delete = "Role.Delete";
        }

        /// <summary>
        /// 用户管理
        /// </summary>
        [PermissionDescription("TenantManage", "User", "用户管理", 3)]
        public static class UserPermissions
        {
            [Description("查看")]
            public const string View = "User.View";
            [Description("创建")]
            public const string Create = "User.Create";
            [Description("编辑")]
            public const string Edit = "User.Edit";
            [Description("删除")]
            public const string Delete = "User.Delete";
        }

        /// <summary>
        /// 系统设置
        /// </summary>
        [PermissionDescription("", "SystemSetting", "系统设置", 1)]
        public static class SystemSettingPermissions
        {
            [Description("查看")]
            public const string View = "SystemSetting.View";
        }

        /// <summary>
        /// 菜单管理
        /// </summary>
        [PermissionDescription("SystemSetting", "Menu", "菜单管理", 0)]
        public static class MenuPermissions
        {
            [Description("查看")]
            public const string View = "Menu.View";
            [Description("创建")]
            public const string Create = "Menu.Create";
            [Description("编辑")]
            public const string Edit = "Menu.Edit";
            [Description("删除")]
            public const string Delete = "Menu.Delete";
        }
    }
}