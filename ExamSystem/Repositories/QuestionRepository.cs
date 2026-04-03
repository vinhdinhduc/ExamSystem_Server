using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly ExamSystemDbContext _context;

    public QuestionRepository(ExamSystemDbContext context)
    {
        _context = context;
    }

    public Task<bool> SubjectExistsAsync(int subjectId)
        => _context.Subjects.AnyAsync(s => s.Id == subjectId);

    public Task<bool> UserExistsAsync(Guid userId)
        => _context.Users.AnyAsync(u => u.Id == userId);

    public Task<List<Question>> GetBySubjectIdAsync(int? subjectId)
    {
        var query = _context.Questions
            .AsNoTracking()
            .Where(q => q.IsActive);

        if (subjectId.HasValue)
        {
            query = query.Where(q => q.SubjectId == subjectId.Value);
        }

        return query
            .Include(q => q.Answers)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public Task AddAsync(Question question)
        => _context.Questions.AddAsync(question).AsTask();

    public Task SaveChangesAsync()
        => _context.SaveChangesAsync();
}