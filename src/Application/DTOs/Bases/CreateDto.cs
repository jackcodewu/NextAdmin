using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs.Bases
{
    /// <summary>
    /// 创建操作的基础DTO
    /// </summary>
    public class CreateDto
    {
        /// <summary>
        /// 名称
        /// </summary>
        [Required(ErrorMessage = "名称不能为空")]
        [StringLength(100, ErrorMessage = "名称长度不能超过100")]
        public string Name { get; set; } = string.Empty;
    }
}
