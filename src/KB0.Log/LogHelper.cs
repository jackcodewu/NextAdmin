using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Linq;

namespace NextAdmin.Log
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    /// <summary>
    /// 日志项类
    /// </summary>
    public class LogItem
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }
        public bool IsConsoleOutput { get; set; }

        public LogItem(LogLevel level, string message, Exception exception = null, string category = null, bool isConsoleOutput = false)
        {
            Level = level;
            Message = message;
            Exception = exception;
            Category = category;
            Timestamp = DateTime.Now;
            IsConsoleOutput = isConsoleOutput;
        }
    }

    /// <summary>
    /// 日志帮助类，提供基于队列的异步日志处理
    /// </summary>
    public class LogHelper : IDisposable
    {
        private static readonly ConcurrentQueue<LogItem> _logQueue = new ConcurrentQueue<LogItem>();
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly Task _processTask;
        private static readonly NLog.ILogger _logger;
        private static readonly object _lock = new object();
        private static bool _isDisposed = false;
        private static bool _isInitialized = false;
        private static int _queueSize = 10000; // 默认队列大小
        private static int _flushInterval = 100; // 默认刷新间隔(毫秒)
        private static bool _enableConsoleOutput = true; // 默认启用控制台输出
        private static string _logFilePath = "logs"; // 默认日志文件路径

        public static bool IsDebugEnabled { get; set; }

        /// <summary>
        /// 静态构造函数，初始化日志处理任务
        /// </summary>
        static LogHelper()
        {
            _processTask = Task.Run(ProcessLogQueueAsync);
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// 配置日志服务
        /// </summary>
        public static void ConfigLogService(IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(loggingBuilder =>
            {
                // configure Logging with NLog
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                loggingBuilder.AddNLog(configuration);
            });
        }

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        /// <param name="queueSize">队列大小</param>
        /// <param name="flushInterval">刷新间隔(毫秒)</param>
        /// <param name="enableConsoleOutput">是否启用控制台输出</param>
        /// <param name="logFilePath">日志文件路径</param>
        public static void Initialize(int queueSize = 10000, int flushInterval = 100, bool enableConsoleOutput = true, string logFilePath = "logs")
        {
            if (_isInitialized)
                return;

            lock (_lock)
            {
                if (_isInitialized)
                    return;

                _queueSize = queueSize;
                _flushInterval = flushInterval;
                _enableConsoleOutput = enableConsoleOutput;
                _logFilePath = logFilePath;

                // 确保日志目录存在
                if (!Directory.Exists(_logFilePath))
                {
                    Directory.CreateDirectory(_logFilePath);
                }

                _isInitialized = true;
                _logger.Info("日志系统初始化完成");
            }
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public static void Debug(string message, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Debug, message, null, category, isConsoleOutput);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Info(string message, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Info, message, null, category, isConsoleOutput);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void Warn(string message, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Warn, message, null, category, isConsoleOutput);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string message, Exception exception = null, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Error, message, exception, category, isConsoleOutput);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(Exception exception, string message,string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Error, message, exception, category, isConsoleOutput);
        }

        /// <summary>
        /// 记录致命错误日志
        /// </summary>
        public static void Fatal(string message, Exception exception = null, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Fatal, message, exception, category, isConsoleOutput);
        }

        /// <summary>
        /// 控制台输出
        /// </summary>
        public static void ConsoleOutput(string message, ConsoleColor color = ConsoleColor.White)
        {
            if (!_enableConsoleOutput)
                return;

            // 直接输出消息，不添加额外格式
            Console.WriteLine(message);
        }

        /// <summary>
        /// 将日志项加入队列
        /// </summary>
        private static void EnqueueLog(LogLevel level, string message, Exception exception = null, string category = null, bool isConsoleOutput = false)
        {
            if (_isDisposed)
                return;

            // 检查队列大小，防止内存溢出
            if (_logQueue.Count >= _queueSize)
            {
                // 队列已满，记录警告并丢弃日志
                _logger.Warn($"日志队列已满({_queueSize})，丢弃日志: {message}");
                return;
            }

            var logItem = new LogItem(level, message, exception, category, isConsoleOutput);
            _logQueue.Enqueue(logItem);
        }

        /// <summary>
        /// 处理日志队列
        /// </summary>
        private static async Task ProcessLogQueueAsync()
        {
            var logBuilder = new StringBuilder();
            var lastFlushTime = DateTime.Now;

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // 批量处理日志
                    while (_logQueue.TryDequeue(out var logItem))
                    {
                        // 构建日志消息
                        var logMessage = BuildLogMessage(logItem);
                        
                        // 根据日志级别记录到NLog
                        switch (logItem.Level)
                        {
                            case LogLevel.Debug:
                                _logger.Debug(logMessage);
                                break;
                            case LogLevel.Info:
                                _logger.Info(logMessage);
                                break;
                            case LogLevel.Warn:
                                _logger.Warn(logMessage);
                                break;
                            case LogLevel.Error:
                                _logger.Error(logItem.Exception, logMessage);
                                break;
                            case LogLevel.Fatal:
                                _logger.Fatal(logItem.Exception, logMessage);
                                break;
                        }

                        // 如果需要控制台输出
                        if (logItem.IsConsoleOutput && _enableConsoleOutput)
                        {
                            Console.WriteLine(logMessage);
                        }

                        // 将日志添加到构建器
                        logBuilder.AppendLine(logMessage);
                    }

                    // 定期刷新日志到文件
                    if (logBuilder.Length > 0 && (DateTime.Now - lastFlushTime).TotalMilliseconds >= _flushInterval)
                    {
                        await FlushLogsToFileAsync(logBuilder);
                        logBuilder.Clear();
                        lastFlushTime = DateTime.Now;
                    }

                    // 短暂休眠，避免CPU占用过高
                    await Task.Delay(10, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    // 记录处理日志时的异常
                    _logger.Error(ex, "处理日志队列时发生异常");
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
            }

            // 处理剩余的日志
            if (logBuilder.Length > 0)
            {
                await FlushLogsToFileAsync(logBuilder);
            }
        }

        /// <summary>
        /// 构建日志消息
        /// </summary>
        private static string BuildLogMessage(LogItem logItem)
        {
            var sb = new StringBuilder();

            //// 只在非控制台输出时添加时间戳和日志级别
            if (!logItem.IsConsoleOutput)
            {
                sb.Append($"\r\n\r\n[{logItem.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logItem.Level}]\r\n");
            }

            if (!string.IsNullOrEmpty(logItem.Category))
            {
                sb.Append($" [{logItem.Category}]");
            }
            
            sb.Append($" {logItem.Message}");
            
            if (logItem.Exception != null)
            {
                sb.AppendLine();
                sb.Append($"Exception: {logItem.Exception}");
                if (logItem.Exception.StackTrace != null)
                {
                    sb.AppendLine();
                    sb.Append($"StackTrace: {logItem.Exception.StackTrace}");
                }
            }
            
            return sb.ToString();
        }

        // ... existing code ...

        /// <summary>
        /// 将日志刷新到文件
        /// </summary>
        private static async Task FlushLogsToFileAsync(StringBuilder logBuilder)
        {
            try
            {
                // 创建按年/月/日分层的目录结构
                var now = DateTime.Now;
                var yearDir = now.ToString("yyyy");
                var monthDir = now.ToString("MM");
                var dayDir = now.ToString("dd");

                var logDir = Path.Combine(_logFilePath, yearDir, monthDir, dayDir);

                // 确保目录存在
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // 生成日志文件名：HH.log (每小时一个文件)
                var logFileName = Path.Combine(logDir, $"{now.ToString("yyyy-MM-dd_HH")}.log");
                await File.AppendAllTextAsync(logFileName, logBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "将日志写入文件时发生异常");
            }
        }

        // ... existing code ...

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                // 取消日志处理任务
                _cancellationTokenSource.Cancel();
                
                // 等待任务完成
                try
                {
                    _processTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "等待日志处理任务完成时发生异常");
                }
                
                // 释放资源
                _cancellationTokenSource.Dispose();
            }

            _isDisposed = true;
        }
    }
}
