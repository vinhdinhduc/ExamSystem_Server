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

public record ExamSessionReviewDto(
    Guid SessionId,
    Guid ExamId,
    Guid UserId,
    string ExamTitle,
    decimal Score,
    bool IsPassed,
    int TotalCorrect,
    DateTime StartedAt,
    DateTime SubmittedAt,
    List<ExamSessionReviewQuestionDto> Questions);

public record ExamSessionReviewQuestionDto(
    Guid QuestionId,
    string Content,
    string? Explanation,
    int OrderIndex,
    decimal Score,
    bool? IsCorrect,
    List<int> SelectedAnswerIds,
    List<int> CorrectAnswerIds,
    List<ExamSessionReviewOptionDto> Options);

public record ExamSessionReviewOptionDto(
    int Id,
    string Content,
    string? ImageUrl,
    int OrderIndex,
    bool IsSelected,
    bool? IsCorrect);

public record StudentExamResultItemDto(
    Guid SessionId,
    Guid ExamId,
    string ExamTitle,
    decimal Score,
    bool IsPassed,
    int TotalCorrect,
    DateTime SubmittedAt,
    byte Status,
    int AttemptNumber);

public record TeacherAssignedStudentResultDto(
    Guid UserId,
    string FullName,
    string Email,
    bool IsSubmitted,
    Guid? SessionId,
    decimal? Score,
    bool? IsPassed,
    DateTime? SubmittedAt,
    int Attempts);

public record TeacherAssignedExamResultDto(
    Guid ExamId,
    string ExamTitle,
    int TotalAssigned,
    int TotalSubmitted,
    int TotalNotSubmitted,
    List<TeacherAssignedStudentResultDto> Students);
