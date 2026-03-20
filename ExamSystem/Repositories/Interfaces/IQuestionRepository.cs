using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface IQuestionRepository
{
    Task<bool> SubjectExistsAsync(int subjectId);
    Task<bool> UserExistsAsync(Guid userId);
    Task<List<Question>> GetBySubjectIdAsync(int? subjectId);
    Task AddAsync(Question question);
    Task SaveChangesAsync();
}