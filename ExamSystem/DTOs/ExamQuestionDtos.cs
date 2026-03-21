using System;
using System.Collections.Generic;

namespace ExamSystem.DTOs;

public record ExamQuestionDto(
    int Id,
    Guid ExamId,
    Guid QuestionId,
    int OrderIndex,
    decimal Score);

public record ExamQuestionDetailOptionDto(
    int Id,
    string Content,
    string? ImageUrl,
    int OrderIndex,
    bool IsCorrect);

public record ExamQuestionDetailDto(
    int ExamQuestionId,
    Guid ExamId,
    Guid QuestionId,
    string Content,
    bool IsCorrect,
    string? ImageUrl,
    byte QuestionType,
    byte DifficultyLevel,
    string? Tags,
    string? Explanation,
    int OrderIndex,
    decimal Score,
    List<ExamQuestionDetailOptionDto> Options);

public record ExamQuestionCreateDto(
    Guid QuestionId,
    int? OrderIndex,
    decimal? Score);

public record SyncExamQuestionsDto(
    List<ExamQuestionCreateDto> Items);

public record ExamQuestionUpdateDto(
    int OrderIndex,
    decimal Score);

public record ReorderExamQuestionsDto(
    List<ExamQuestionOrderItemDto> Items);

public record ExamQuestionOrderItemDto(
    int ExamQuestionId,
    int OrderIndex);
