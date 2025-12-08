using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Core.Domain.Entities.Sys;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs.Roles
{
    /// <summary>
    /// Update role DTO
    /// </summary>
    public class UpdateRoleDto : UpdateDto
    {

        /// <summary>
        /// Role description
        /// </summary>
        [StringLength(200, ErrorMessage = "Role description length cannot exceed 200 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Tenant ID (optional)
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Menu list
        /// </summary>
        public List<Menu> Menus { get; set; }

        /// <summary>
        /// Permission list
        /// </summary>
        public List<Permission> Permissions { get; set; }
    }
} 
