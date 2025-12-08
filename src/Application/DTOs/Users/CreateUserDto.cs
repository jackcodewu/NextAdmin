using NextAdmin.Application.DTOs.Bases;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs
{
    /// <summary>
    /// Create user DTO
    /// </summary>
    public class CreateUserDto 
    {
        /// <summary>
        /// Username
        /// </summary>
        [Required(ErrorMessage = "Username cannot be empty")]
        [StringLength(50, ErrorMessage = "Username length cannot exceed 50 characters")]
        public required string UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [Required(ErrorMessage = "Password cannot be empty")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password length must be between 6-100 characters")]
        public required string Password { get; set; }

        /// <summary>
        /// User role ID list
        /// </summary>
        public List<string> RoleIds { get; set; } = new();
    }
}
