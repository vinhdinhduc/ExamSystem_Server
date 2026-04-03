using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface IExamSessionRepository
{
    Task<Exam?> GetExamForStartAsync(Guid examId);
    Task<int> CountAttemptsAsync(Guid examId, Guid userId);
    Task<List<Guid>> GetAssignedExamIdsAsync(Guid userId);

    Task AddSessionAsync(ExamSession session);
    Task<ExamSession?> GetSessionByIdAsync(Guid sessionId);
    Task<ExamSession?> GetActiveSessionForUserExamAsync(Guid examId, Guid userId, DateTime utcNow);
    Task<ExamSession?> GetSessionForReviewAsync(Guid sessionId);
    Task<List<ExamSession>> GetSessionsByExamAndUsersAsync(List<Guid> examIds, List<Guid> userIds);
    Task<SessionAnswer?> GetSessionAnswerAsync(Guid sessionId, Guid questionId);
    Task<List<SessionAnswer>> GetSessionAnswersWithDetailsAsync(Guid sessionId);

    Task AddSessionAnswerDetailAsync(SessionAnswerDetail detail);
    Task RemoveSessionAnswerDetailsAsync(List<SessionAnswerDetail> details);
    Task SaveChangesAsync();

    Task<List<ExamSession>> GetExpiredActiveSessionsAsync(DateTime utcNow);
}