using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DTOs.Roles
{
    public class RolesDto : BasesDto
    {

        /// <summary>
        /// Role description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// User count
        /// </summary>
        public int UserCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
