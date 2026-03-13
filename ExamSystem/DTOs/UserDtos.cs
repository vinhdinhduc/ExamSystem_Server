using System;

namespace ExamSystem.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? Avatar,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UserCreateDto(
    string Username,
    string Email,
    string PasswordHash,
    string FullName,
    string? Avatar,
    bool IsActive);

public record UserUpdateDto(
    string Email,
    string PasswordHash,
    string FullName,
    string? Avatar,
    bool IsActive);
