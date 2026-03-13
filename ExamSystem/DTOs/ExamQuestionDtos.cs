using System;

namespace ExamSystem.DTOs;

public record ExamQuestionDto(
    int Id,
    Guid ExamId,
    Guid QuestionId,
    int OrderIndex,
    decimal Score);

public record ExamQuestionCreateDto(
    Guid ExamId,
    Guid QuestionId,
    int OrderIndex,
    decimal Score);

public record ExamQuestionUpdateDto(
    int OrderIndex,
    decimal Score);
