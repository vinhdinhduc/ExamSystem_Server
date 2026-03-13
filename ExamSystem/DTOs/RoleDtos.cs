using System;

namespace ExamSystem.DTOs;

public record RoleDto(
    int Id,
    string Name,
    bool IsDefault,
    DateTime CreatedAt);

public record RoleCreateDto(
    string Name,
    bool IsDefault);

public record RoleUpdateDto(
    string Name,
    bool IsDefault);
