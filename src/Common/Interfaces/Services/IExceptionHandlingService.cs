namespace NextAdmin.Common.Interfaces.Services
{
    /// <summary>
    /// Exception handling service interface
    /// </summary>
    public interface IExceptionHandlingService
    {
        /// <summary>
        /// Handle exception
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <returns>Handling result</returns>
        Task<ExceptionHandlingResult> HandleExceptionAsync(Exception exception);

        /// <summary>
        /// Get exception message
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <returns>Exception message</returns>
        string GetExceptionMessage(Exception exception);
    }

    /// <summary>
    /// Exception handling result
    /// </summary>
    public class ExceptionHandlingResult
    {
        /// <summary>
        /// Whether handled
        /// </summary>
        public bool IsHandled { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error code
        /// </summary>
        public string? ErrorCode { get; set; }
    }
} 
