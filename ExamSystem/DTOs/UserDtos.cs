using System;
using System.Collections.Generic;

namespace ExamSystem.DTOs;

// DTO để trả về thông tin User (không kèm roles — dùng nội bộ)
public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    DateTime CreatedAt
);

// DTO trả về danh sách users kèm roles — dùng cho trang Admin
public record UserListItemDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    DateTime CreatedAt,
    List<string> Roles
);

// DTO để tạo User mới
public record UserCreateDto(
    string Username,
    string Email,
    string Password,
    string FullName
);

// DTO để cập nhật User
public record UserUpdateDto(
    string? Username,
    string? Email,
    string? FullName,
    bool? IsActive
);

// DTO để đổi password
public record UserChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);

// DTO để trả về User kèm Roles
public record UserWithRolesDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    DateTime CreatedAt,
    List<RoleDto> Roles
);

// DTO để gán Roles cho User
public record AssignRolesToUserDto(
    List<Guid> RoleIds
);

