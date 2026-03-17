using System;
using System.Collections.Generic;

namespace ExamSystem.DTOs;

public record ExamDto(
    Guid Id,
    int SubjectId,
    Guid CreatedByUserId,
    string Title,
    string? Description,
    string? Instructions,
    int Duration,
    int TotalQuestions,
    decimal PassScore,
    int MaxAttempts,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    bool ShowResultAfter,
    bool ShowCorrectAnswer,
    byte Status,
    DateTime? StartDate,
    DateTime? EndDate,
    string? AccessCode,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ExamQuestionDto>? Questions = null);

public record ExamCreateDto(
    int SubjectId,
    Guid CreatedByUserId,
    string Title,
    string? Description,
    string? Instructions,
    int Duration,
    int TotalQuestions,
    decimal PassScore,
    int MaxAttempts,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    bool ShowResultAfter,
    bool ShowCorrectAnswer,
    byte Status,
    DateTime? StartDate,
    DateTime? EndDate,
    string? AccessCode);

public record ExamUpdateDto(
    string Title,
    string? Description,
    string? Instructions,
    int Duration,
    int TotalQuestions,
    decimal PassScore,
    int MaxAttempts,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    bool ShowResultAfter,
    bool ShowCorrectAnswer,
    byte Status,
    DateTime? StartDate,
    DateTime? EndDate,
    string? AccessCode);

public record ExamFilterDto(
    int? SubjectId,
    byte? Status,
    string? Keyword,
    int? Page,
    int? PageSize);

public record PublishExamDto(
    Guid PublishedByUserId);
