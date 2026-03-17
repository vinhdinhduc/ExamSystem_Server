using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly ExamSystemDbContext _context;

    public SubjectRepository(ExamSystemDbContext context)
    {
        _context = context;
    }

    public Task<Subject?> GetByIdAsync(int id)
        => _context.Subjects.FirstOrDefaultAsync(s => s.Id == id);

    public Task<Subject?> GetByCodeAsync(string code)
        => _context.Subjects.FirstOrDefaultAsync(s => s.Code == code);

    public async Task<(List<Subject> Items, int Total)> GetPagedAsync(string? keyword, bool? isActive, int page, int pageSize)
    {
        var query = _context.Subjects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(s => s.Name.Contains(keyword) || s.Code.Contains(keyword));
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        query = query.OrderBy(s => s.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Subject> CreateAsync(Subject subject)
    {
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task<Subject> UpdateAsync(Subject subject)
    {
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
        {
            return false;
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        return true;
    }
}
