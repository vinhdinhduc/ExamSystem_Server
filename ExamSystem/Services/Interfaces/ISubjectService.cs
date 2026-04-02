using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface ISubjectService
{
    Task<(List<SubjectDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(SubjectFilterDto filter);
    Task<SubjectDto?> GetByIdAsync(int id);
    Task<SubjectDto> CreateAsync(SubjectCreateDto dto);
    Task<SubjectDto> UpdateAsync(int id, SubjectUpdateDto dto);
    Task<SubjectDto> ToggleActiveAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}