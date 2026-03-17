using Microsoft.AspNetCore.Authorization;

namespace ExamSystem.Authorization;

/// <summary>
/// Kiểm tra user có permission cụ thể không (thông qua roles).
/// Dùng: [RequirePermission("USER_VIEW")]
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(policy: $"Permission:{permission}")
    {
    }
}
