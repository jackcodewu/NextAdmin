namespace NextAdmin.Common.Interfaces.Services
{
    /// <summary>
    /// Logging service interface
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Log information message
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="args">Arguments</param>
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// Log warning message
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="args">Arguments</param>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Log error message
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="args">Arguments</param>
        void LogError(string message, params object[] args);

        /// <summary>
        /// Log error message with exception
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="message">Log message</param>
        /// <param name="args">Arguments</param>
        void LogError(Exception exception, string message, params object[] args);
    }
} 
