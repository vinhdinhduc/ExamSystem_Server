using System;

namespace ExamSystem.DTOs;

public record RefreshTokenDto(
    int Id,
    Guid UserId,
    string Token,
    DateTime ExpiresAt,
    bool IsRevoked,
    string? DeviceInfo,
    string? IpAddress,
    DateTime CreatedAt);

public record RefreshTokenCreateDto(
    Guid UserId,
    string Token,
    DateTime ExpiresAt,
    bool IsRevoked,
    string? DeviceInfo,
    string? IpAddress);
