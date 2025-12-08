using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace NextAdmin.Common.Helpers
{
    /// <summary>
    /// Log helper class
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Log information message
        /// </summary>
        public static void LogInformation(ILogger logger, string message, Dictionary<string, object> data = null)
        {
            if (logger == null) return;
            logger.LogInformation(FormatMessage(message, data));
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void LogWarning(ILogger logger, string message, Dictionary<string, object> data = null)
        {
            if (logger == null) return;
            logger.LogWarning(FormatMessage(message, data));
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void LogError(ILogger logger, Exception ex, string message, Dictionary<string, object> data = null)
        {
            if (logger == null) return;
            logger.LogError(ex, FormatMessage(message, data));
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        public static void LogDebug(ILogger logger, string message, Dictionary<string, object> data = null)
        {
            if (logger == null) return;
            logger.LogDebug(FormatMessage(message, data));
        }

        /// <summary>
        /// Format log message
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
