namespace NextAdmin.Common.Interfaces.Services
{
    /// <summary>
    /// 异常处理服务接口
    /// </summary>
    public interface IExceptionHandlingService
    {
        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="exception">异常</param>
        /// <returns>处理结果</returns>
        Task<ExceptionHandlingResult> HandleExceptionAsync(Exception exception);

        /// <summary>
        /// 获取异常信息
        /// </summary>
        /// <param name="exception">异常</param>
        /// <returns>异常信息</returns>
        string GetExceptionMessage(Exception exception);
    }

    /// <summary>
    /// 异常处理结果
    /// </summary>
    public class ExceptionHandlingResult
    {
        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool IsHandled { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public string? ErrorCode { get; set; }
    }
} 
