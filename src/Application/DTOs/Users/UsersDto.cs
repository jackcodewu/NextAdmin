using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DTOs.Users
{
    public class UsersDto: BasesDto
    {  /// <summary>
       /// Username
       /// </summary>
        public required string UserName { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// First name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Department
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Position
        /// </summary>
        public string? Position { get; set; }

        /// <summary>
        /// Is active
        /// </summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last login time
        /// </summary>
        public DateTime? LastLoginTime { get; set; }

    }
}
