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
