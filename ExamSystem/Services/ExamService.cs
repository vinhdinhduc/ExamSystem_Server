using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class ExamService : IExamService
{
    private readonly IExamRepository _examRepository;
    private readonly IMapper _mapper;

    public ExamService(IExamRepository examRepository, IMapper mapper)
    {
        _examRepository = examRepository;
        _mapper = mapper;
    }

    public async Task<(List<ExamDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(ExamFilterDto filter)
    {
        var page = filter.Page.GetValueOrDefault(1);
        var pageSize = filter.PageSize.GetValueOrDefault(20);
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var (items, total) = await _examRepository.GetPagedAsync(filter.SubjectId, filter.Status, filter.Keyword, page, pageSize);
        return (_mapper.Map<List<ExamDto>>(items), total, page, pageSize);
    }

    public async Task<ExamDto?> GetByIdAsync(Guid id)
    {
        var exam = await _examRepository.GetByIdWithDetailsAsync(id);
        return exam == null ? null : _mapper.Map<ExamDto>(exam);
    }

    public async Task<ExamDto> CreateAsync(ExamCreateDto dto)
    {
        var exam = _mapper.Map<Exam>(dto);
        exam.CreatedAt = DateTime.UtcNow;
        exam.UpdatedAt = DateTime.UtcNow;

        var created = await _examRepository.CreateAsync(exam);
        return _mapper.Map<ExamDto>(created);
    }

    public async Task<ExamDto> UpdateAsync(Guid id, ExamUpdateDto dto)
    {
        var exam = await _examRepository.GetByIdAsync(id);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with id '{id}' not found");
        }

        exam.Title = dto.Title;
        exam.Description = dto.Description;
        exam.Instructions = dto.Instructions;
        exam.Duration = dto.Duration;
        exam.TotalQuestions = dto.TotalQuestions;
        exam.PassScore = dto.PassScore;
        exam.MaxAttempts = dto.MaxAttempts;
        exam.ShuffleQuestions = dto.ShuffleQuestions;
        exam.ShuffleAnswers = dto.ShuffleAnswers;
        exam.ShowResultAfter = dto.ShowResultAfter;
        exam.ShowCorrectAnswer = dto.ShowCorrectAnswer;
        exam.Status = dto.Status;
        exam.StartDate = dto.StartDate;
        exam.EndDate = dto.EndDate;
        exam.AccessCode = dto.AccessCode;
        exam.UpdatedAt = DateTime.UtcNow;

        var updated = await _examRepository.UpdateAsync(exam);
        return _mapper.Map<ExamDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await _examRepository.DeleteAsync(id);
        if (!deleted)
        {
            throw new KeyNotFoundException($"Exam with id '{id}' not found");
        }
    }

    public async Task<List<ExamQuestionDto>> GetExamQuestionsAsync(Guid examId)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with id '{examId}' not found");
        }

        var questions = await _examRepository.GetExamQuestionsAsync(examId);
        return _mapper.Map<List<ExamQuestionDto>>(questions);
    }

    public async Task<ExamQuestionDto> AddQuestionAsync(Guid examId, ExamQuestionCreateDto dto)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with id '{examId}' not found");
        }

        var existing = await _examRepository.GetExamQuestionsAsync(examId);
        if (existing.Any(q => q.QuestionId == dto.QuestionId))
        {
            throw new InvalidOperationException("Question already exists in exam");
        }

        var examQuestion = new ExamQuestion
        {
            ExamId = examId,
            QuestionId = dto.QuestionId,
            OrderIndex = dto.OrderIndex,
            Score = dto.Score
        };

        await _examRepository.AddExamQuestionAsync(examQuestion);
        await _examRepository.SaveChangesAsync();

        return _mapper.Map<ExamQuestionDto>(examQuestion);
    }

    public async Task RemoveQuestionAsync(Guid examId, int examQuestionId)
    {
        var examQuestion = await _examRepository.GetExamQuestionByIdAsync(examQuestionId);
        if (examQuestion == null || examQuestion.ExamId != examId)
        {
            throw new KeyNotFoundException("Exam question not found");
        }

        await _examRepository.RemoveExamQuestionAsync(examQuestion);
        await _examRepository.SaveChangesAsync();
    }

    public async Task ReorderQuestionsAsync(Guid examId, ReorderExamQuestionsDto dto)
    {
        var examQuestions = await _examRepository.GetExamQuestionsAsync(examId);
        var map = examQuestions.ToDictionary(x => x.Id, x => x);

        foreach (var item in dto.Items)
        {
            if (!map.TryGetValue(item.ExamQuestionId, out var examQuestion))
            {
                throw new KeyNotFoundException($"ExamQuestion '{item.ExamQuestionId}' not found in exam");
            }

            examQuestion.OrderIndex = item.OrderIndex;
        }

        await _examRepository.SaveChangesAsync();
    }

    public async Task PublishAsync(Guid examId, PublishExamDto dto)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with id '{examId}' not found");
        }

        var hasPermission = await _examRepository.HasUserPermissionAsync(dto.PublishedByUserId, "EXAM_PUBLISH");
        if (!hasPermission)
        {
            throw new UnauthorizedAccessException("User does not have EXAM_PUBLISH permission");
        }

        exam.Status = 1;
        exam.UpdatedAt = DateTime.UtcNow;

        await _examRepository.UpdateAsync(exam);
    }

    public async Task AssignAsync(Guid examId, ExamAssignmentCreateDto dto)
    {
        if (!dto.UserId.HasValue && !dto.GroupId.HasValue)
        {
            throw new InvalidOperationException("Must assign exam to user or group");
        }

        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with id '{examId}' not found");
        }

        var assignment = new ExamAssignment
        {
            ExamId = examId,
            UserId = dto.UserId,
            GroupId = dto.GroupId,
            AssignedAt = DateTime.UtcNow
        };

        await _examRepository.AssignExamAsync(assignment);
        await _examRepository.SaveChangesAsync();
    }

    public async Task<List<StudentAssignedExamDto>> GetStudentAssignedExamsAsync(Guid userId)
    {
        var assignments = await _examRepository.GetStudentAssignmentsAsync(userId);

        return assignments
            .Where(a => a.Exam != null)
            .Select(a => new StudentAssignedExamDto(
                a.ExamId,
                a.Exam.Title,
                a.Exam.Duration,
                a.Exam.PassScore,
                a.Exam.StartDate,
                a.Exam.EndDate,
                a.Exam.Status,
                a.AssignedAt))
            .ToList();
    }
}
