using System;
using System.Collections.Generic;

namespace ExamSystem.DTOs;

public record ExamQuestionDto(
    int Id,
    Guid ExamId,
    Guid QuestionId,
    int OrderIndex,
    decimal Score);

public record ExamQuestionCreateDto(
    Guid QuestionId,
    int OrderIndex,
    decimal Score);

public record ExamQuestionUpdateDto(
    int OrderIndex,
    decimal Score);

public record ReorderExamQuestionsDto(
    List<ExamQuestionOrderItemDto> Items);

public record ExamQuestionOrderItemDto(
    int ExamQuestionId,
    int OrderIndex);
