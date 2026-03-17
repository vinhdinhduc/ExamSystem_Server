using System;

namespace ExamSystem.DTOs;

public record SubjectDto(
    int Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);

public record SubjectCreateDto(
    string Name,
    string Code,
    string? Description,
    bool IsActive);

public record SubjectUpdateDto(
    string Name,
    string Code,
    string? Description,
    bool IsActive);

public record SubjectFilterDto(
    string? Keyword,
    bool? IsActive,
    int? Page,
    int? PageSize);
