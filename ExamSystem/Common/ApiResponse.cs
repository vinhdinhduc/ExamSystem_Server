namespace ExamSystem.Common;

public class ApiResponse<T>
{
    public int StatusCode { get; set; }
    public object? Error { get; set; }
    public object Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T? data, string message = "Success", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            StatusCode = statusCode,
            Error = null,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Failure(object? error, string message, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            StatusCode = statusCode,
            Error = error,
            Message = message,
            Data = default
        };
    }
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Pages { get; set; }
    public int Total { get; set; }
}

public class PaginatedResult<T>
{
    public PaginationMeta Meta { get; set; } = null!;
    public List<T> Result { get; set; } = new();
}
