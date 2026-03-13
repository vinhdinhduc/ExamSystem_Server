using System;

namespace ExamSystem.DTOs;

public record QuestionDto(
    Guid Id,
    int SubjectId,
    Guid CreatedByUserId,
    string Content,
    string? ImageUrl,
    byte QuestionType,
    byte DifficultyLevel,
    string? Tags,
    string? Explanation,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record QuestionCreateDto(
    int SubjectId,
    Guid CreatedByUserId,
    string Content,
    string? ImageUrl,
    byte QuestionType,
    byte DifficultyLevel,
    string? Tags,
    string? Explanation,
    bool IsActive);

public record QuestionUpdateDto(
    string Content,
    string? ImageUrl,
    byte QuestionType,
    byte DifficultyLevel,
    string? Tags,
    string? Explanation,
    bool IsActive);
