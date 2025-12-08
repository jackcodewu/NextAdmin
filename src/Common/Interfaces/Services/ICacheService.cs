namespace NextAdmin.Common.Interfaces.Services
{
    /// <summary>
    /// Cache service interface
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get cached value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached value</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Set cached value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Cache value</param>
        /// <param name="expiration">Expiration time</param>
        /// <returns>Set result</returns>
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Remove cached value
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Remove result</returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// Check if cache key exists
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Whether exists</returns>
        Task<bool> ExistsAsync(string key);
    }
} 
