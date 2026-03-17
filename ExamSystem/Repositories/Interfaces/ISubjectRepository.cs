using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(int id);
    Task<Subject?> GetByCodeAsync(string code);
    Task<(List<Subject> Items, int Total)> GetPagedAsync(string? keyword, bool? isActive, int page, int pageSize);
    Task<Subject> CreateAsync(Subject subject);
    Task<Subject> UpdateAsync(Subject subject);
    Task<bool> DeleteAsync(int id);
}