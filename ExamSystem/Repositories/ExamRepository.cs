using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class ExamRepository : IExamRepository
{
    private readonly ExamSystemDbContext _context;

    public ExamRepository(ExamSystemDbContext context)
    {
        _context = context;
    }

    public Task<Exam?> GetByIdAsync(Guid id)
        => _context.Exams.FirstOrDefaultAsync(e => e.Id == id);

    public Task<Exam?> GetByIdWithDetailsAsync(Guid id)
        => _context.Exams
            .Include(e => e.ExamQuestions.OrderBy(q => q.OrderIndex))
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<(List<Exam> Items, int Total)> GetPagedAsync(int? subjectId, byte? status, string? keyword, int page, int pageSize)
    {
        var query = _context.Exams.AsQueryable();

        if (subjectId.HasValue)
        {
            query = query.Where(e => e.SubjectId == subjectId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(e => e.Title.Contains(keyword));
        }

        query = query.OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Exam> CreateAsync(Exam exam)
    {
        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();
        return exam;
    }

    public async Task<Exam> UpdateAsync(Exam exam)
    {
        _context.Exams.Update(exam);
        await _context.SaveChangesAsync();
        return exam;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null)
        {
            return false;
        }

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<List<ExamQuestion>> GetExamQuestionsAsync(Guid examId)
        => _context.ExamQuestions
            .Where(eq => eq.ExamId == examId)
            .OrderBy(eq => eq.OrderIndex)
            .ToListAsync();

    public Task<ExamQuestion?> GetExamQuestionByIdAsync(int examQuestionId)
        => _context.ExamQuestions.FirstOrDefaultAsync(eq => eq.Id == examQuestionId);

    public Task AddExamQuestionAsync(ExamQuestion examQuestion)
        => _context.ExamQuestions.AddAsync(examQuestion).AsTask();

    public Task RemoveExamQuestionAsync(ExamQuestion examQuestion)
    {
        _context.ExamQuestions.Remove(examQuestion);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<bool> HasUserPermissionAsync(Guid userId, string permissionCode)
        => _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p)
            .AnyAsync(p => p.Code == permissionCode);

    public Task AssignExamAsync(ExamAssignment assignment)
        => _context.ExamAssignments.AddAsync(assignment).AsTask();

    public Task<List<ExamAssignment>> GetStudentAssignmentsAsync(Guid userId)
        => _context.ExamAssignments
            .Include(a => a.Exam)
            .Where(a => a.UserId == userId ||
                        (a.GroupId != null && _context.GroupMembers.Any(gm => gm.GroupId == a.GroupId && gm.UserId == userId)))
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();
}
