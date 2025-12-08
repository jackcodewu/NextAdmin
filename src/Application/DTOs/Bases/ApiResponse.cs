using System.Text.Json.Serialization;

namespace NextAdmin.Application.DTOs
{ 
    /// <summary>
    /// API response model
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Is successful
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Response code
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "200";

        /// <summary>
        /// Response message
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Response data
        /// </summary>
        [JsonPropertyName("data")]
        public T Data { get; set; }

        /// <summary>
        /// Create success response
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = "Operation successful")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Code = "200",
                Message = message,
                Data = data
            };
        }      
        /// <summary>
        /// Create error response
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string code, string message, T data = default)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Code = code,
                Message = message,
                Data = data
            };
        }
    }

    public class ApiResponse : ApiResponse<string>
    {
        public static ApiResponse SuccessResponse(string message = "Operation successful")
        {
            return new ApiResponse
            {
                Success = true,
                Code = "200",
                Message = message,
                Data = null
            };
        }

        public static ApiResponse ErrorResponse(string code, string message="Operation failed")
        {
            return new ApiResponse
            {
                Success = false,
                Code = code,
                Message = message,
                Data = null
            };
        }
    }
} 
