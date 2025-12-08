using NextAdmin.Core.Domain.Entities;
using System.Linq.Expressions;

namespace NextAdmin.Application.DTOs.Users
{
    /// <summary>
    /// User query DTO
    /// </summary>
    public class UserQueryDto
    {
        // public int PageNumber { get; set; } = 1;
        // public int PageSize { get; set; } = 20;

        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Department { get; set; }

        public Expression<Func<ApplicationUser, bool>> ToExpression()
        {
            return u =>
                (string.IsNullOrEmpty(UserName) || u.UserName.Contains(UserName)) &&
                (string.IsNullOrEmpty(Email) || u.Email.Contains(Email)) &&
                (string.IsNullOrEmpty(Department) || (u.Department != null && u.Department.Contains(Department)));
        }
    }
} 
