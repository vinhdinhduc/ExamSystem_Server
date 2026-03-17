using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IExamSessionService
{
    Task<StartExamResponseDto> StartExamAsync(Guid examId, StartExamRequestDto dto, string? ipAddress);
    Task AutoSaveAnswerAsync(Guid sessionId, AutoSaveAnswerDto dto);
    Task<SubmitExamResultDto> SubmitAsync(Guid sessionId, SubmitExamDto dto);
    Task<int> AutoSubmitExpiredSessionsAsync();
}