namespace ExamSystem.DTOs;

public record PermissionDto(
    int Id,
    string Name,
    string? Description,
    string Module);

public record PermissionCreateDto(
    string Name,
    string? Description,
    string Module);

public record PermissionUpdateDto(
    string Name,
    string? Description,
    string Module);
