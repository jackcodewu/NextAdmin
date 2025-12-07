namespace NextAdmin.Common.Interfaces.Services
{
    /// <summary>
    /// 性能监控服务接口
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// 开始监控
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>监控ID</returns>
        string StartMonitoring(string operationName);

        /// <summary>
        /// 结束监控
        /// </summary>
        /// <param name="monitoringId">监控ID</param>
        void StopMonitoring(string monitoringId);

        /// <summary>
        /// 获取性能指标
        /// </summary>
        /// <returns>性能指标列表</returns>
        Task<List<PerformanceMetric>> GetMetricsAsync();
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>
        /// 操作名称
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTime { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
} 
