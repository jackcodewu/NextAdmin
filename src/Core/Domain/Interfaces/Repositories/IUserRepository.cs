using System.Collections.Generic;
using System.Threading.Tasks;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Interfaces.Repositories;

namespace NextAdmin.Core.Domain.Interfaces.Repositories
{
    /// <summary>
    /// 用户仓储接口
    /// </summary>
    public interface IUserRepository : IBaseRepository<User>
    {
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// 根据邮箱获取用户
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// 根据手机号获取用户
        /// </summary>
        Task<User?> GetByPhoneAsync(string phone);

        /// <summary>
        /// 根据状态获取用户列表
        /// </summary>
        Task<List<User>> GetByStatusAsync(UserStatus status);

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        Task<bool> ExistsByUsernameAsync(string username);

        /// <summary>
        /// 检查邮箱是否存在
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email);
    }
}
