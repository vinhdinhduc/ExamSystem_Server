using ExamSystem.DTOs;

namespace ExamSystem.Services;

public interface IExamService
{
    Task<List<ExamDto>> GetAllAsync();
    Task<ExamDto> GetByIdAsync(Guid id);
    Task<ExamDto> CreateAsync(ExamCreateDto dto);
    Task<ExamDto> UpdateAsync(Guid id, ExamUpdateDto dto);
    Task DeleteAsync(Guid id);
}
