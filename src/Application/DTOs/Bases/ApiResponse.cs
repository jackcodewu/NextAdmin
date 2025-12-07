using System.Text.Json.Serialization;

namespace NextAdmin.Application.DTOs
{ 
    /// <summary>
    /// API响应模型
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 响应码
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "200";

        /// <summary>
        /// 响应消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        [JsonPropertyName("data")]
        public T Data { get; set; }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = "操作成功")
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
        /// 创建失败响应
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
        public static ApiResponse SuccessResponse(string message = "操作成功")
        {
            return new ApiResponse
            {
                Success = true,
                Code = "200",
                Message = message,
                Data = null
            };
        }

        public static ApiResponse ErrorResponse(string code, string message="操作失败")
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
