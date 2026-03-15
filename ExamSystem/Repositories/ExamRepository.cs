using ExamSystem.Data;
using ExamSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class ExamRepository : Repository<Exam>, IExamRepository
{
    public ExamRepository(ExamSystemDbContext context) : base(context)
    {
    }

    public Task<List<Exam>> GetAllWithDetailsAsync()
    {
        return Context.Exams
            .AsNoTracking()
            .Include(exam => exam.Subject)
            .Include(exam => exam.CreatedByUser)
            .ToListAsync();
    }

    public Task<Exam?> GetByIdWithDetailsAsync(Guid id)
    {
        return Context.Exams
            .Include(exam => exam.Subject)
            .Include(exam => exam.CreatedByUser)
            .FirstOrDefaultAsync(exam => exam.Id == id);
    }
}
