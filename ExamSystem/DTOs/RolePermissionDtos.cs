namespace ExamSystem.DTOs;

public record RolePermissionDto(
    int RoleId,
    int PermissionId);

public record RolePermissionCreateDto(
    int RoleId,
    int PermissionId);
