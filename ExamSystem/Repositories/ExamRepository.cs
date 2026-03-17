using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class ExamRepository : Repository<Exam>, IExamRepository
{
    public ExamRepository(ExamSystemDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<List<Exam>> GetAllWithDetailsAsync()
    {
        return Context.Exams
            .AsNoTracking()
            .Include(exam => exam.Subject)
            .Include(exam => exam.CreatedByUser)
            .ToListAsync();

        return (items, total);
    }

    public Task<Exam?> GetByIdWithDetailsAsync(Guid id)
    {
        return Context.Exams
            .Include(exam => exam.Subject)
            .Include(exam => exam.CreatedByUser)
            .FirstOrDefaultAsync(exam => exam.Id == id);
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
