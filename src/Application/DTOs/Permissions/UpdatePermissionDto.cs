using NextAdmin.Application.DTOs.Bases;

namespace NextAdmin.Application.DTOs.Permissions
{
    /// <summary>
    /// Update permission DTO
    /// </summary>
    public class UpdatePermissionDto : UpdateDto
    {
        
        public string Code { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Child permissions
        /// </summary>
        public List<UpdatePermissionDto> Children { get; set; } = new();
    }
} 
