namespace ProjectName.Api.Models.Shared
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }

        public ApiResponse(bool success, T? data = default, string? message = null, Dictionary<string, string[]>? errors = null)
        {
            Success = success;
            Data = data;
            Message = message;
            Errors = errors;
        }

        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>(true, data, message);
        }

        public static ApiResponse<T> ErrorResponse(string message, Dictionary<string, string[]>? errors = null)
        {
            return new ApiResponse<T>(false, default, message, errors);
        }
    }
}