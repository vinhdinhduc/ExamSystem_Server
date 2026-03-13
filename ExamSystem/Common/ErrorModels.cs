namespace ExamSystem.Common;

public class ValidationError
{
    public string Code => "VALIDATION_ERROR";
    public List<ValidationDetail> Details { get; set; } = new();
}

public class ValidationDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class BusinessError
{
    public string Code => "BUSINESS_ERROR";
    public string Reason { get; set; } = string.Empty;
}

public class NotFoundError
{
    public string Code => "NOT_FOUND";
    public string Resource { get; set; } = string.Empty;
}

public class UnauthorizedError
{
    public string Code => "UNAUTHORIZED";
    public string Reason { get; set; } = "Authentication required";
}

public class ForbiddenError
{
    public string Code => "FORBIDDEN";
    public string Reason { get; set; } = "Access denied";
}

public class InternalServerError
{
    public string Code => "INTERNAL_SERVER_ERROR";
    public string Reason { get; set; } = "Unexpected server error";
}
