using NextAdmin.Application.DTOs.Bases;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs
{
    /// <summary>
    /// 创建用户DTO
    /// </summary>
    public class CreateUserDto 
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
        public required string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
        public required string Password { get; set; }

        /// <summary>
        /// 用户角色ID列表
        /// </summary>
        public List<string> RoleIds { get; set; } = new();
    }
}
