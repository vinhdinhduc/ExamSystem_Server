namespace ExamSystem.Common;

/// <summary>
/// Cấu hình tài khoản Admin mặc định — chỉnh sửa trong appsettings.json
/// </summary>
public class AdminSettings
{
    public string Username { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}