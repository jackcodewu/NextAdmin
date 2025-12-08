namespace NextAdmin.Common.Interfaces.Services
{
    /// <summary>
    /// Performance monitoring service interface
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// Start monitoring
        /// </summary>
        /// <param name="operationName">Operation name</param>
        /// <returns>Monitoring ID</returns>
        string StartMonitoring(string operationName);

        /// <summary>
        /// Stop monitoring
        /// </summary>
        /// <param name="monitoringId">Monitoring ID</param>
        void StopMonitoring(string monitoringId);

        /// <summary>
        /// Get performance metrics
        /// </summary>
        /// <returns>List of performance metrics</returns>
        Task<List<PerformanceMetric>> GetMetricsAsync();
    }

    /// <summary>
    /// Performance metric
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>
        /// Operation name
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Execution time (milliseconds)
        /// </summary>
        public long ExecutionTime { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Whether successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
} 
