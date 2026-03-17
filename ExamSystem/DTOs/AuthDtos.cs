using System;
using System.Collections.Generic;

namespace ExamSystem.DTOs;

// UserDto kèm roles — dùng trong auth response để FE biết roles ngay sau login
public record UserInfoDto(
    Guid Id,
    string Username,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    List<string> Roles
);

// DTO để đăng ký user mới — chỉ cần Email + Password, Username/FullName tự sinh
public record RegisterDto(
    string Email,
    string Password,
    string? Username = null,
    string? FullName = null
);

// DTO để đăng nhập
public record LoginDto(
    string UsernameOrEmail,
    string Password
);

// DTO response sau khi login/register thành công
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfoDto User
);

// DTO để refresh token request
public record RefreshTokenRequestDto(
    string RefreshToken
);

// DTO để logout
public record LogoutDto(
    string RefreshToken
);

// DTO để yêu cầu quên mật khẩu
public record ForgotPasswordDto(
    string Email
);

// DTO để xác nhận reset mật khẩu bằng OTP
public record ResetPasswordDto(
    string Email,
    string Otp,
    string NewPassword,
    string ConfirmPassword
);

// DTO để verify email
public record VerifyEmailDto(
    string Token
);
