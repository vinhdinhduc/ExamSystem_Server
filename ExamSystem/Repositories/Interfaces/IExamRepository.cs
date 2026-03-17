using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface IExamRepository
{
    Task<Exam?> GetByIdAsync(Guid id);
    Task<Exam?> GetByIdWithDetailsAsync(Guid id);
    Task<(List<Exam> Items, int Total)> GetPagedAsync(int? subjectId, byte? status, string? keyword, int page, int pageSize);
    Task<Exam> CreateAsync(Exam exam);
    Task<Exam> UpdateAsync(Exam exam);
    Task<bool> DeleteAsync(Guid id);

    Task<List<ExamQuestion>> GetExamQuestionsAsync(Guid examId);
    Task<ExamQuestion?> GetExamQuestionByIdAsync(int examQuestionId);
    Task AddExamQuestionAsync(ExamQuestion examQuestion);
    Task RemoveExamQuestionAsync(ExamQuestion examQuestion);
    Task SaveChangesAsync();

    Task<bool> HasUserPermissionAsync(Guid userId, string permissionCode);
    Task AssignExamAsync(ExamAssignment assignment);
    Task<List<ExamAssignment>> GetStudentAssignmentsAsync(Guid userId);
}