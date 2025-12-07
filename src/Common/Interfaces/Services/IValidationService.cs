namespace NextAdmin.Common.Interfaces.Services
{
    /// <summary>
    /// 验证服务接口
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// 验证对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要验证的对象</param>
        /// <returns>验证结果</returns>
        Task<bool> ValidateAsync<T>(T obj);

        /// <summary>
        /// 获取验证错误
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要验证的对象</param>
        /// <returns>验证错误列表</returns>
        Task<List<string>> GetValidationErrorsAsync<T>(T obj);
    }
} 
