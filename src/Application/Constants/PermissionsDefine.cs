using System.ComponentModel;

namespace NextAdmin.Application.Constants
{
    /// <summary>
    /// Permission definitions
    /// Example permission definitions, can be extended based on actual business requirements
    /// </summary>
    public static class PermissionsDefine
    {
        public const string GroupName = "NextAdmin";

        /// <summary>
        /// Tenant Management
        /// </summary>
        [PermissionDescription("", "TenantManage", "Tenant Management", 0)]
        public static class TenantManagePermissions
        {
            [Description("View")]
            public const string View = "TenantManage.View";
        }

        /// <summary>
        /// Tenant
        /// </summary>
        [PermissionDescription("TenantManage", "Tenant", "Tenant", 0)]
        public static class TenantPermissions
        {
            [Description("View")]
            public const string View = "Tenant.View";
            [Description("Create")]
            public const string Create = "Tenant.Create";
            [Description("Edit")]
            public const string Edit = "Tenant.Edit";
            [Description("Delete")]
            public const string Delete = "Tenant.Delete";
        }

        /// <summary>
        /// Permission Management
        /// </summary>
        [PermissionDescription("TenantManage", "Permission", "Permission Management", 1)]
        public static class PermissionPermissions
        {
            [Description("View")]
            public const string View = "Permission.View";
            [Description("Create")]
            public const string Create = "Permission.Create";
            [Description("Edit")]
            public const string Edit = "Permission.Edit";
            [Description("Delete")]
            public const string Delete = "Permission.Delete";
        }

        /// <summary>
        /// Role Management
        /// </summary>
        [PermissionDescription("TenantManage", "Role", "Role Management", 2)]
        public static class RolePermissions
        {
            [Description("View")]
            public const string View = "Role.View";
            [Description("Create")]
            public const string Create = "Role.Create";
            [Description("Edit")]
            public const string Edit = "Role.Edit";
            [Description("Delete")]
            public const string Delete = "Role.Delete";
        }

        /// <summary>
        /// User Management
        /// </summary>
        [PermissionDescription("TenantManage", "User", "User Management", 3)]
        public static class UserPermissions
        {
            [Description("View")]
            public const string View = "User.View";
            [Description("Create")]
            public const string Create = "User.Create";
            [Description("Edit")]
            public const string Edit = "User.Edit";
            [Description("Delete")]
            public const string Delete = "User.Delete";
        }

        /// <summary>
        /// System Settings
        /// </summary>
        [PermissionDescription("", "SystemSetting", "System Settings", 1)]
        public static class SystemSettingPermissions
        {
            [Description("View")]
            public const string View = "SystemSetting.View";
        }

        /// <summary>
        /// Menu Management
        /// </summary>
        [PermissionDescription("SystemSetting", "Menu", "Menu Management", 0)]
        public static class MenuPermissions
        {
            [Description("View")]
            public const string View = "Menu.View";
            [Description("Create")]
            public const string Create = "Menu.Create";
            [Description("Edit")]
            public const string Edit = "Menu.Edit";
            [Description("Delete")]
            public const string Delete = "Menu.Delete";
        }
    }
}