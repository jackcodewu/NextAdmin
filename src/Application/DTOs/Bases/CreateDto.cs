using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs.Bases
{
    /// <summary>
    /// Base DTO for create operations
    /// </summary>
    public class CreateDto
    {
        /// <summary>
        /// Name
        /// </summary>
        [Required(ErrorMessage = "Name cannot be empty")]
        [StringLength(100, ErrorMessage = "Name length cannot exceed 100")]
        public string Name { get; set; } = string.Empty;
    }
}
