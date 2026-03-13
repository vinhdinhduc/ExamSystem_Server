using System;

namespace ExamSystem.DTOs;

public record GroupDto(
    int Id,
    string Name,
    string Code,
    string? Description,
    Guid CreatedByUserId,
    DateTime CreatedAt);

public record GroupCreateDto(
    string Name,
    string Code,
    string? Description,
    Guid CreatedByUserId);

public record GroupUpdateDto(
    string Name,
    string Code,
    string? Description);
