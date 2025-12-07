using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace NextAdmin.Common.Helpers
{
    /// <summary>
    /// 日志帮助类
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void LogInformation(ILogger logger, string message, Dictionary<string, object> data = null)
        {
            if (logger == null) return;
            logger.LogInformation(FormatMessage(message, data));
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void LogWarning(ILogger logger, string message, Dictionary<string, object> data = null)
        {
            if (logger == null) return;
            logger.LogWarning(FormatMessage(message, data));
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void LogError(ILogger logger, Exception ex, string message, Dictionary<string, object> data = null)
        {
            if (logger == null) return;
            logger.LogError(ex, FormatMessage(message, data));
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public static void LogDebug(ILogger logger, string message, Dictionary<string, object> data = null)
        {
            if (logger == null) return;
            logger.LogDebug(FormatMessage(message, data));
        }

        /// <summary>
        /// 格式化日志消息
        /// </summary>
        private static string FormatMessage(string message, Dictionary<string, object> data)
        {
            if (data == null || data.Count == 0)
            {
                return message;
            }

            var formattedData = new List<string>();
            foreach (var item in data)
            {
                formattedData.Add($"{item.Key}={item.Value}");
            }

            return $"{message} | {string.Join(", ", formattedData)}";
        }
    }
} 
