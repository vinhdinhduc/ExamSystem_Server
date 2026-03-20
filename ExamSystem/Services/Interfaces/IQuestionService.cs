using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IQuestionService
{
    Task<List<QuestionDto>> GetAsync(int? subjectId);
    Task<QuestionDto> CreateAsync(QuestionCreateDto dto);
}