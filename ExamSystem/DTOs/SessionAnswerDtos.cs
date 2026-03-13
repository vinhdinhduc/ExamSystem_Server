using System;

namespace ExamSystem.DTOs;

public record SessionAnswerDto(
    int Id,
    Guid SessionId,
    Guid QuestionId,
    string CorrectAnswerIds,
    bool? IsCorrect,
    decimal Score,
    DateTime? AnsweredAt);

public record SessionAnswerCreateDto(
    Guid SessionId,
    Guid QuestionId,
    string CorrectAnswerIds,
    decimal Score,
    DateTime? AnsweredAt);

public record SessionAnswerUpdateDto(
    bool? IsCorrect,
    decimal Score,
    DateTime? AnsweredAt);
