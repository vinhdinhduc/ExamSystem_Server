using ExamSystem.DTOs;
using ExamSystem.Mappings;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;

    public QuestionService(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<List<QuestionDto>> GetAsync(int? subjectId)
    {
        var questions = await _questionRepository.GetBySubjectIdAsync(subjectId);
        return questions.Select(QuestionEntityMapper.ToDto).ToList();
    }

    public async Task<QuestionDto> CreateAsync(QuestionCreateDto dto)
    {
        if (!dto.CreatedByUserId.HasValue)
        {
            throw new InvalidOperationException("Thiếu thông tin người tạo câu hỏi");
        }

        var subjectExists = await _questionRepository.SubjectExistsAsync(dto.SubjectId);
        if (!subjectExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy môn học với id '{dto.SubjectId}'");
        }

        var userExists = await _questionRepository.UserExistsAsync(dto.CreatedByUserId.Value);
        if (!userExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy người dùng với id '{dto.CreatedByUserId}'");
        }

        var now = DateTime.UtcNow;
        var question = new Question
        {
            Id = Guid.NewGuid(),
            SubjectId = dto.SubjectId,
            CreatedByUserId = dto.CreatedByUserId.Value,
            Content = dto.Content,
            ImageUrl = dto.ImageUrl,
            QuestionType = dto.QuestionType,
            DifficultyLevel = dto.DifficultyLevel,
            Tags = dto.Tags,
            Explanation = dto.Explanation,
            IsActive = dto.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
            Answers = new List<Answer>()
        };

        if (dto.Options is { Count: > 0 })
        {
            var normalizedOptions = dto.Options
                .OrderBy(o => o.OrderIndex)
                .Select((o, index) => new Answer
                {
                    Content = o.Content,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = index,
                    ImageUrl = o.ImageUrl
                })
                .ToList();

            if (!normalizedOptions.Any(o => o.IsCorrect))
            {
                normalizedOptions[0].IsCorrect = true;
            }

            question.Answers = normalizedOptions;
        }

        await _questionRepository.AddAsync(question);
        await _questionRepository.SaveChangesAsync();

        return QuestionEntityMapper.ToDto(question);
    }
}