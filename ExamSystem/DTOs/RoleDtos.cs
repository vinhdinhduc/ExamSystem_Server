using System;
using System.Collections.Generic;

namespace ExamSystem.DTOs;

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt);

public record RoleWithPermissionsDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    List<PermissionDto> Permissions);

public record RoleCreateDto(
    string Name,
    string? Description);

public record RoleUpdateDto(
    string Name,
    string? Description);

public record AssignPermissionsToRoleDto(
    List<Guid> PermissionIds);
