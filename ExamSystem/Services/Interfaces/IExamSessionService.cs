using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IExamSessionService
{
    Task<StartExamResponseDto> StartExamAsync(Guid examId, StartExamRequestDto dto, string? ipAddress);
    Task AutoSaveAnswerAsync(Guid sessionId, AutoSaveAnswerDto dto);
    Task<SaveExamProgressResultDto> SaveProgressAsync(Guid sessionId, SaveExamProgressDto dto);
    Task<SubmitExamResultDto> SubmitAsync(Guid sessionId, SubmitExamDto dto);
    Task<ExamViolationResultDto> ReportViolationAsync(ExamViolationDto dto);
    Task<ExamSessionReviewDto> GetReviewAsync(Guid sessionId, Guid userId);
    Task<List<StudentExamResultItemDto>> GetStudentResultsAsync(Guid userId);
    Task<List<TeacherAssignedExamResultDto>> GetTeacherAssignedResultsAsync(Guid teacherId, Guid? examId = null);
    Task<int> AutoSubmitExpiredSessionsAsync();
    /// <summary>Tạm dừng phiên không còn heartbeat (phát hiện mất mạng/treo từ phía server).</summary>
    Task<int> PauseSessionsWithStaleHeartbeatAsync(CancellationToken cancellationToken = default);
    Task<SystemInterruptionResultDto> ReportSystemInterruptionAsync(SystemInterruptionReportDto dto);
    Task HeartbeatAsync(ExamSessionHeartbeatDto dto);
    Task<SessionRuntimeStatusDto> GetSessionRuntimeStatusAsync(Guid sessionId, Guid userId);
    Task<List<PendingSystemPauseItemDto>> GetPendingSystemPausesAsync(Guid viewerUserId, bool viewerIsAdmin);
    Task<AdminResolvePauseResultDto> AdminResolveSystemPauseAsync(
        Guid sessionId,
        AdminResolveSessionPauseRequestDto dto,
        Guid resolverUserId,
        bool resolverIsAdmin);
}