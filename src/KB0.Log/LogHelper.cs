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
    /// Log level enumeration
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
    /// Log item class
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
    /// Log helper class that provides queue-based asynchronous log processing
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
        private static int _queueSize = 10000; // Default queue size
        private static int _flushInterval = 100; // Default flush interval (milliseconds)
        private static bool _enableConsoleOutput = true; // Default enable console output
        private static string _logFilePath = "logs"; // Default log file path

        public static bool IsDebugEnabled { get; set; }

        /// <summary>
        /// Static constructor to initialize log processing task
        /// </summary>
        static LogHelper()
        {
            _processTask = Task.Run(ProcessLogQueueAsync);
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Configure log service
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
        /// Initialize log system
        /// </summary>
        /// <param name="queueSize">Queue size</param>
        /// <param name="flushInterval">Flush interval (milliseconds)</param>
        /// <param name="enableConsoleOutput">Enable console output</param>
        /// <param name="logFilePath">Log file path</param>
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

                // Ensure log directory exists
                if (!Directory.Exists(_logFilePath))
                {
                    Directory.CreateDirectory(_logFilePath);
                }

                _isInitialized = true;
                _logger.Info("Log system initialization completed");
            }
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        public static void Debug(string message, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Debug, message, null, category, isConsoleOutput);
        }

        /// <summary>
        /// Log information message
        /// </summary>
        public static void Info(string message, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Info, message, null, category, isConsoleOutput);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void Warn(string message, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Warn, message, null, category, isConsoleOutput);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void Error(string message, Exception exception = null, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Error, message, exception, category, isConsoleOutput);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void Error(Exception exception, string message,string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Error, message, exception, category, isConsoleOutput);
        }

        /// <summary>
        /// Log fatal error message
        /// </summary>
        public static void Fatal(string message, Exception exception = null, string category = null, bool isConsoleOutput = false)
        {
            EnqueueLog(LogLevel.Fatal, message, exception, category, isConsoleOutput);
        }

        /// <summary>
        /// Console output
        /// </summary>
        public static void ConsoleOutput(string message, ConsoleColor color = ConsoleColor.White)
        {
            if (!_enableConsoleOutput)
                return;

            // Output message directly without additional formatting
            Console.WriteLine(message);
        }

        /// <summary>
        /// Enqueue log item to queue
        /// </summary>
        private static void EnqueueLog(LogLevel level, string message, Exception exception = null, string category = null, bool isConsoleOutput = false)
        {
            if (_isDisposed)
                return;

            // Check queue size to prevent memory overflow
            if (_logQueue.Count >= _queueSize)
            {
                // Queue is full, log warning and discard log
                _logger.Warn($"Log queue is full ({_queueSize}), discarding log: {message}");
                return;
            }

            var logItem = new LogItem(level, message, exception, category, isConsoleOutput);
            _logQueue.Enqueue(logItem);
        }

        /// <summary>
        /// Process log queue
        /// </summary>
        private static async Task ProcessLogQueueAsync()
        {
            var logBuilder = new StringBuilder();
            var lastFlushTime = DateTime.Now;

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Batch process logs
                    while (_logQueue.TryDequeue(out var logItem))
                    {
                        // Build log message
                        var logMessage = BuildLogMessage(logItem);
                        
                        // Log to NLog based on log level
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

                        // If console output is needed
                        if (logItem.IsConsoleOutput && _enableConsoleOutput)
                        {
                            Console.WriteLine(logMessage);
                        }

                        // Add log to builder
                        logBuilder.AppendLine(logMessage);
                    }

                    // Periodically flush logs to file
                    if (logBuilder.Length > 0 && (DateTime.Now - lastFlushTime).TotalMilliseconds >= _flushInterval)
                    {
                        await FlushLogsToFileAsync(logBuilder);
                        logBuilder.Clear();
                        lastFlushTime = DateTime.Now;
                    }

                    // Brief sleep to avoid high CPU usage
                    await Task.Delay(10, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    // Log exceptions during log processing
                    _logger.Error(ex, "Exception occurred while processing log queue");
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
            }

            // Process remaining logs
            if (logBuilder.Length > 0)
            {
                await FlushLogsToFileAsync(logBuilder);
            }
        }

        /// <summary>
        /// Build log message
        /// </summary>
        private static string BuildLogMessage(LogItem logItem)
        {
            var sb = new StringBuilder();

            //// Add timestamp and log level only for non-console output
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
        /// Flush logs to file
        /// </summary>
        private static async Task FlushLogsToFileAsync(StringBuilder logBuilder)
        {
            try
            {
                // Create hierarchical directory structure by year/month/day
                var now = DateTime.Now;
                var yearDir = now.ToString("yyyy");
                var monthDir = now.ToString("MM");
                var dayDir = now.ToString("dd");

                var logDir = Path.Combine(_logFilePath, yearDir, monthDir, dayDir);

                // Ensure directory exists
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // Generate log file name: HH.log (one file per hour)
                var logFileName = Path.Combine(logDir, $"{now.ToString("yyyy-MM-dd_HH")}.log");
                await File.AppendAllTextAsync(logFileName, logBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception occurred while writing logs to file");
            }
        }

        // ... existing code ...

        /// <summary>
        /// Release resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                // Cancel log processing task
                _cancellationTokenSource.Cancel();
                
                // Wait for task completion
                try
                {
                    _processTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Exception occurred while waiting for log processing task to complete");
                }
                
                // Release resources
                _cancellationTokenSource.Dispose();
            }

            _isDisposed = true;
        }
    }
}
