using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IExamSessionService
{
    Task<StartExamResponseDto> StartExamAsync(Guid examId, StartExamRequestDto dto, string? ipAddress);
    Task AutoSaveAnswerAsync(Guid sessionId, AutoSaveAnswerDto dto);
    Task<SubmitExamResultDto> SubmitAsync(Guid sessionId, SubmitExamDto dto);
    Task<ExamSessionReviewDto> GetReviewAsync(Guid sessionId, Guid userId);
    Task<List<StudentExamResultItemDto>> GetStudentResultsAsync(Guid userId);
    Task<List<TeacherAssignedExamResultDto>> GetTeacherAssignedResultsAsync(Guid teacherId, Guid? examId = null);
    Task<int> AutoSubmitExpiredSessionsAsync();
}