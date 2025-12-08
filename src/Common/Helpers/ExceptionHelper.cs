using System;
using System.Collections.Generic;

namespace NextAdmin.Common.Helpers
{
    /// <summary>
    /// Exception helper class
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Get exception details
        /// </summary>
        public static Dictionary<string, object> GetExceptionDetails(Exception ex)
        {
            var details = new Dictionary<string, object>
            {
                { "Message", ex.Message },
                { "StackTrace", ex.StackTrace },
                { "Source", ex.Source },
                { "Type", ex.GetType().Name }
            };

            if (ex.InnerException != null)
            {
                details["InnerException"] = GetExceptionDetails(ex.InnerException);
            }

            return details;
        }

        /// <summary>
        /// Check if exception is retryable
        /// </summary>
        public static bool IsRetryableException(Exception ex)
        {
            return ex is TimeoutException ||
                   ex is System.Net.Http.HttpRequestException ||
                   ex is System.IO.IOException;
        }

        /// <summary>
        /// Get error code from exception
        /// </summary>
        public static string GetErrorCode(Exception ex)
        {
            switch (ex)
            {
                case ArgumentException:
                    return "ARGUMENT_ERROR";
                //case ArgumentNullException:
                //    return "ARGUMENT_NULL_ERROR";
                case InvalidOperationException:
                    return "INVALID_OPERATION_ERROR";
                case TimeoutException:
                    return "TIMEOUT_ERROR";
                case System.Net.Http.HttpRequestException:
                    return "HTTP_REQUEST_ERROR";
                case System.IO.IOException:
                    return "IO_ERROR";
                default:
                    return "UNKNOWN_ERROR";
            }
        }
    }
} 
