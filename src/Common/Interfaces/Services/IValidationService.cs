namespace NextAdmin.Common.Interfaces.Services
{
    /// <summary>
    /// Validation service interface
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Validate object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object to validate</param>
        /// <returns>Validation result</returns>
        Task<bool> ValidateAsync<T>(T obj);

        /// <summary>
        /// Get validation errors
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object to validate</param>
        /// <returns>List of validation errors</returns>
        Task<List<string>> GetValidationErrorsAsync<T>(T obj);
    }
} 
