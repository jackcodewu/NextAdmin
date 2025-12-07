using NextAdmin.Application.DTOs.Bases;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// 更新权限DTO
    /// </summary>
    public class UpdatePermissionDto : UpdateDto
    {
        
        public string Code { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// 子权限
        /// </summary>
        public List<UpdatePermissionDto> Children { get; set; } = new();
    }
} 
