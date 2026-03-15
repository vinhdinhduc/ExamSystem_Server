using System;
using System.ComponentModel.DataAnnotations;

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
    DateTime UpdatedAt);

public record ExamCreateDto(
    [property: Range(1, int.MaxValue)] int SubjectId,
    [property: Required] Guid CreatedByUserId,
    [property: Required, StringLength(200)] string Title,
    [property: StringLength(2000)] string? Description,
    string? Instructions,
    [property: Range(1, 10000)] int Duration,
    [property: Range(1, 10000)] int TotalQuestions,
    [property: Range(0, 100)] decimal PassScore,
    [property: Range(0, 100)] int MaxAttempts,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    bool ShowResultAfter,
    bool ShowCorrectAnswer,
    byte Status,
    DateTime? StartDate,
    DateTime? EndDate,
    [property: StringLength(50)] string? AccessCode);

public record ExamUpdateDto(
    [property: Required, StringLength(200)] string Title,
    [property: StringLength(2000)] string? Description,
    string? Instructions,
    [property: Range(1, 10000)] int Duration,
    [property: Range(1, 10000)] int TotalQuestions,
    [property: Range(0, 100)] decimal PassScore,
    [property: Range(0, 100)] int MaxAttempts,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    bool ShowResultAfter,
    bool ShowCorrectAnswer,
    byte Status,
    DateTime? StartDate,
    DateTime? EndDate,
    [property: StringLength(50)] string? AccessCode);
