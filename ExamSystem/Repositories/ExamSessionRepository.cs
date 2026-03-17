using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class ExamSessionRepository : IExamSessionRepository
{
    private readonly ExamSystemDbContext _context;

    public ExamSessionRepository(ExamSystemDbContext context)
    {
        _context = context;
    }

    public Task<Exam?> GetExamForStartAsync(Guid examId)
        => _context.Exams
            .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(e => e.Id == examId);

    public Task<int> CountAttemptsAsync(Guid examId, Guid userId)
        => _context.ExamSessions.CountAsync(s => s.ExamId == examId && s.UserId == userId);

    public async Task<List<Guid>> GetAssignedExamIdsAsync(Guid userId)
    {
        var directExamIds = _context.ExamAssignments
            .Where(a => a.UserId == userId)
            .Select(a => a.ExamId);

        var groupIds = _context.GroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId);

        var groupExamIds = _context.ExamAssignments
            .Where(a => a.GroupId != null && groupIds.Contains(a.GroupId.Value))
            .Select(a => a.ExamId);

        return await directExamIds
            .Union(groupExamIds)
            .Distinct()
            .ToListAsync();
    }

    public Task AddSessionAsync(ExamSession session)
        => _context.ExamSessions.AddAsync(session).AsTask();

    public Task<ExamSession?> GetSessionByIdAsync(Guid sessionId)
        => _context.ExamSessions
            .Include(s => s.Exam)
                .ThenInclude(e => e.ExamQuestions)
            .Include(s => s.SessionAnswers)
                .ThenInclude(sa => sa.SessionAnswerDetails)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

    public Task<SessionAnswer?> GetSessionAnswerAsync(Guid sessionId, Guid questionId)
        => _context.SessionAnswers
            .Include(sa => sa.SessionAnswerDetails)
            .FirstOrDefaultAsync(sa => sa.SessionId == sessionId && sa.QuestionId == questionId);

    public Task<List<SessionAnswer>> GetSessionAnswersWithDetailsAsync(Guid sessionId)
        => _context.SessionAnswers
            .Include(sa => sa.SessionAnswerDetails)
            .Where(sa => sa.SessionId == sessionId)
            .ToListAsync();

    public Task AddSessionAnswerDetailAsync(SessionAnswerDetail detail)
        => _context.SessionAnswerDetails.AddAsync(detail).AsTask();

    public Task RemoveSessionAnswerDetailsAsync(List<SessionAnswerDetail> details)
    {
        _context.SessionAnswerDetails.RemoveRange(details);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<List<ExamSession>> GetExpiredActiveSessionsAsync(DateTime utcNow)
        => _context.ExamSessions
            .Include(s => s.Exam)
                .ThenInclude(e => e.ExamQuestions)
            .Include(s => s.SessionAnswers)
                .ThenInclude(sa => sa.SessionAnswerDetails)
            .Where(s => s.Status == 0 && s.ExpiresAt <= utcNow)
            .ToListAsync();
}
