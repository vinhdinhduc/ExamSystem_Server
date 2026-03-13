using System;

namespace ExamSystem.DTOs;

public record UserRoleDto(
    Guid UserId,
    int RoleId,
    DateTime AssignedAt);

public record UserRoleCreateDto(
    Guid UserId,
    int RoleId);
