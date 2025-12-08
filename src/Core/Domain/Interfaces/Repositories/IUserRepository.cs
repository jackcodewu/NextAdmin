using System.Collections.Generic;
using System.Threading.Tasks;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Interfaces.Repositories;

namespace NextAdmin.Core.Domain.Interfaces.Repositories
{
    /// <summary>
    /// User repository interface
    /// </summary>
    public interface IUserRepository : IBaseRepository<User>
    {
        /// <summary>
        /// Get user by username
        /// </summary>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Get user by email
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Get user by phone
        /// </summary>
        Task<User?> GetByPhoneAsync(string phone);

        /// <summary>
        /// Get user list by status
        /// </summary>
        Task<List<User>> GetByStatusAsync(UserStatus status);

        /// <summary>
        /// Check if username exists
        /// </summary>
        Task<bool> ExistsByUsernameAsync(string username);

        /// <summary>
        /// Check if email exists
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email);
    }
}
