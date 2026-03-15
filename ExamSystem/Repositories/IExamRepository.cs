using ExamSystem.Models;

namespace ExamSystem.Repositories;

public interface IExamRepository : IRepository<Exam>
{
    Task<List<Exam>> GetAllWithDetailsAsync();
    Task<Exam?> GetByIdWithDetailsAsync(Guid id);
}
