using Microsoft.AspNetCore.Http;

namespace ExamSystem.DTOs;

public record ExamOptionDraftDto(
    string Content,
    bool IsCorrect,
    int OrderIndex,
    string? ImageUrl = null);

public record ExamQuestionDraftDto(
    string Content,
    string? Explanation,
    byte QuestionType,
    byte DifficultyLevel,
    decimal Score,
    int OrderIndex,
    List<ExamOptionDraftDto> Options);

public record ExamDraftDto(
    string Title,
    string? Description,
    string? Instructions,
    int Duration,
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
    List<ExamQuestionDraftDto> Questions);

public record GenerateExamWithGeminiRequestDto(
    int SubjectId,
    Guid CreatedByUserId,
    string Title,
    string? Description,
    string? Instructions,
    int QuestionCount,
    byte DifficultyLevel,
    int Duration,
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
    string? AdditionalPrompt,
    bool SaveToDatabase = true);

public record ImportExamFromFileRequestDto(
    int SubjectId,
    Guid CreatedByUserId,
    string Title,
    string? Description,
    string? Instructions,
    int Duration,
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
    IFormFile File,
    bool SaveToDatabase = true);

public record ExamAuthoringResultDto(
    Guid? ExamId,
    string Source,
    int TotalQuestions,
    ExamDraftDto Draft);
