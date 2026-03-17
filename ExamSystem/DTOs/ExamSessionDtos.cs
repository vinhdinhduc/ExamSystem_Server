using System;
using System.Collections.Generic;

namespace ExamSystem.DTOs;

public record ExamSessionDto(
    Guid Id,
    Guid ExamId,
    Guid UserId,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    DateTime ExpiresAt,
    byte Status,
    decimal? Score,
    bool? IsPassed,
    int? TotalCorrect,
    int AttemptNumber,
    string? IpAddress,
    string? QuestionOrder);

public record StartExamRequestDto(
    Guid UserId,
    string? AccessCode);

public record StartExamResponseDto(
    Guid SessionId,
    DateTime StartedAt,
    DateTime ExpiresAt,
    int AttemptNumber,
    List<ExamSessionQuestionDto> Questions);

public record ExamSessionQuestionDto(
    Guid QuestionId,
    int OrderIndex,
    List<int> AnswerIds);

public record AutoSaveAnswerDto(
    Guid UserId,
    Guid QuestionId,
    List<int> AnswerIds);

public record SubmitExamDto(
    Guid UserId);

public record SubmitExamResultDto(
    Guid SessionId,
    decimal Score,
    bool IsPassed,
    int TotalCorrect,
    DateTime SubmittedAt,
    byte Status);
