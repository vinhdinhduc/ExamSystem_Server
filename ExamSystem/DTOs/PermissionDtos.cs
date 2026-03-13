using System;

namespace ExamSystem.DTOs;

public record PermissionDto(
    Guid Id,
    string Code,
    string? Description);

public record PermissionCreateDto(
    string Code,
    string? Description);

public record PermissionUpdateDto(
    string Code,
    string? Description);
