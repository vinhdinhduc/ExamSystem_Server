using System;

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

public record ExamSessionCreateDto(
    Guid ExamId,
    Guid UserId,
    DateTime StartedAt,
    DateTime ExpiresAt,
    byte Status,
    int AttemptNumber,
    string? IpAddress,
    string? QuestionOrder);

public record ExamSessionUpdateDto(
    DateTime? SubmittedAt,
    byte Status,
    decimal? Score,
    bool? IsPassed,
    int? TotalCorrect);
