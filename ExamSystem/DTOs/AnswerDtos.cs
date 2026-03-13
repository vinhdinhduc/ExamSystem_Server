using System;

namespace ExamSystem.DTOs;

public record AnswerDto(
    int Id,
    Guid QuestionId,
    string Content,
    string? ImageUrl,
    bool IsCorrect,
    int OrderIndex);

public record AnswerCreateDto(
    Guid QuestionId,
    string Content,
    string? ImageUrl,
    bool IsCorrect,
    int OrderIndex);

public record AnswerUpdateDto(
    string Content,
    string? ImageUrl,
    bool IsCorrect,
    int OrderIndex);
