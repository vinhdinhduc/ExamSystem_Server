using System;
using System.Collections.Generic;

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

public record QuestionOptionCreateDto(
    string Content,
    bool IsCorrect,
    int OrderIndex,
    string? ImageUrl = null);

public record QuestionCreateDto(
    int SubjectId,
    string Content,
    string? ImageUrl,
    byte QuestionType,
    byte DifficultyLevel,
    string? Tags,
    string? Explanation,
    List<QuestionOptionCreateDto>? Options = null,
    Guid? CreatedByUserId = null,
    bool IsActive = true);

public record QuestionUpdateDto(
    string Content,
    string? ImageUrl,
    byte QuestionType,
    byte DifficultyLevel,
    string? Tags,
    string? Explanation,
    bool IsActive);
