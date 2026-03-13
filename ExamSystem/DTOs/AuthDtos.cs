using System;

namespace ExamSystem.DTOs;

// DTO để đăng ký user mới
public record RegisterDto(
    string Username,
    string Email,
    string Password,
    string FullName
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
    UserDto User
);

// DTO để refresh token request
public record RefreshTokenRequestDto(
    string RefreshToken
);

// DTO để logout
public record LogoutDto(
    string RefreshToken
);
