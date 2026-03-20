using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IExamService
{
    Task<(List<ExamDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(ExamFilterDto filter);
    Task<ExamDto?> GetByIdAsync(Guid id);
    Task<ExamDto> CreateAsync(ExamCreateDto dto);
    Task<ExamDto> UpdateAsync(Guid id, ExamUpdateDto dto);
    Task DeleteAsync(Guid id);

    Task<List<ExamQuestionDto>> GetExamQuestionsAsync(Guid examId);
    Task<List<ExamQuestionDetailDto>> GetExamQuestionDetailsAsync(Guid examId);
    Task<ExamQuestionDto> AddQuestionAsync(Guid examId, ExamQuestionCreateDto dto);
    Task SyncExamQuestionsAsync(Guid examId, SyncExamQuestionsDto dto);
    Task RemoveQuestionAsync(Guid examId, int examQuestionId);
    Task ReorderQuestionsAsync(Guid examId, ReorderExamQuestionsDto dto);

    Task PublishAsync(Guid examId, PublishExamDto dto);
    Task AssignAsync(Guid examId, ExamAssignmentCreateDto dto);
    Task<List<StudentAssignedExamDto>> GetStudentAssignedExamsAsync(Guid userId);
}