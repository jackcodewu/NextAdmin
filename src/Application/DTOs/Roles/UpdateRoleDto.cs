using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Core.Domain.Entities.Sys;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs.Roles
{
    /// <summary>
    /// 更新角色DTO
    /// </summary>
    public class UpdateRoleDto : UpdateDto
    {

        /// <summary>
        /// 角色描述
        /// </summary>
        [StringLength(200, ErrorMessage = "角色描述长度不能超过200个字符")]
        public string? Description { get; set; }

        /// <summary>
        /// 公司ID(可选)
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// 菜单列表
        /// </summary>
        public List<Menu> Menus { get; set; }

        /// <summary>
        /// 权限列表
        /// </summary>
        public List<Permission> Permissions { get; set; }
    }
} 
