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
            throw new KeyNotFoundException($"Đề thi với id '{id}' không tồn tại");
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
            throw new KeyNotFoundException($"Đề thi với id '{id}' không tồn tại");
        }
    }

    public async Task<List<ExamQuestionDto>> GetExamQuestionsAsync(Guid examId)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Đề thi với id '{examId}' không tồn tại");
        }

        var questions = await _examRepository.GetExamQuestionsAsync(examId);
        return _mapper.Map<List<ExamQuestionDto>>(questions);
    }

    public async Task<List<ExamQuestionDetailDto>> GetExamQuestionDetailsAsync(Guid examId)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Đề thi với id '{examId}' không tồn tại");
        }

        var examQuestions = await _examRepository.GetExamQuestionsWithDetailsAsync(examId);

        return examQuestions.Select(eq => new ExamQuestionDetailDto(
            eq.Id,
            eq.ExamId,
            eq.QuestionId,
            eq.Question.Content,
            eq.Question.Answers.Any(a => a.IsCorrect),
            eq.Question.ImageUrl,
            eq.Question.QuestionType,
            eq.Question.DifficultyLevel,
            eq.Question.Tags,
            eq.Question.Explanation,
            eq.OrderIndex,
            eq.Score,
            eq.Question.Answers
                .OrderBy(a => a.OrderIndex)
                .Select(a => new ExamQuestionDetailOptionDto(
                    a.Id,
                    a.Content,
                    a.ImageUrl,
                    a.OrderIndex,
                    a.IsCorrect))
                .ToList()
        )).ToList();
    }

    public async Task<ExamQuestionDto> AddQuestionAsync(Guid examId, ExamQuestionCreateDto dto)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Đề thi với id ' {examId} ' không tồn tại");
        }

        var existing = await _examRepository.GetExamQuestionsAsync(examId);
        if (existing.Any(q => q.QuestionId == dto.QuestionId))
        {
            throw new InvalidOperationException("Câu hỏi đã tồn tại trong đề thi");
        }

        var orderIndex = dto.OrderIndex ?? (existing.Count == 0 ? 1 : existing.Max(q => q.OrderIndex) + 1);
        var score = dto.Score ?? 1;

        if (orderIndex <= 0)
        {
            throw new InvalidOperationException("OrderIndex phải lớn hơn 0");
        }

        if (score <= 0)
        {
            throw new InvalidOperationException("Score phải lớn hơn 0");
        }

        var examQuestion = new ExamQuestion
        {
            ExamId = examId,
            QuestionId = dto.QuestionId,
            OrderIndex = orderIndex,
            Score = score
        };

        await _examRepository.AddExamQuestionAsync(examQuestion);
        await _examRepository.SaveChangesAsync();

        return _mapper.Map<ExamQuestionDto>(examQuestion);
    }

    public async Task SyncExamQuestionsAsync(Guid examId, SyncExamQuestionsDto dto)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Đề thi với id '{examId}' không tồn tại");
        }

        var incomingItems = dto.Items ?? new List<ExamQuestionCreateDto>();

        var duplicateQuestionIds = incomingItems
            .GroupBy(x => x.QuestionId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateQuestionIds.Count > 0)
        {
            throw new InvalidOperationException("Danh sách câu hỏi gửi lên có câu hỏi bị trùng");
        }

        var existing = await _examRepository.GetExamQuestionsAsync(examId);
        var existingByQuestionId = existing.ToDictionary(x => x.QuestionId, x => x);
        var incomingQuestionIds = incomingItems.Select(x => x.QuestionId).ToHashSet();

        var toRemove = existing.Where(x => !incomingQuestionIds.Contains(x.QuestionId)).ToList();
        foreach (var examQuestion in toRemove)
        {
            await _examRepository.RemoveExamQuestionAsync(examQuestion);
        }

        for (var index = 0; index < incomingItems.Count; index++)
        {
            var item = incomingItems[index];
            var orderIndex = item.OrderIndex ?? index + 1;
            var score = item.Score ?? 1;

            if (orderIndex <= 0)
            {
                throw new InvalidOperationException("Thứ tự câu hỏi phải lớn hơn 0");
            }

            if (score <= 0)
            {
                throw new InvalidOperationException("Điểm câu hỏi phải lớn hơn 0");
            }

            if (existingByQuestionId.TryGetValue(item.QuestionId, out var examQuestion))
            {
                examQuestion.OrderIndex = orderIndex;
                examQuestion.Score = score;
            }
            else
            {
                await _examRepository.AddExamQuestionAsync(new ExamQuestion
                {
                    ExamId = examId,
                    QuestionId = item.QuestionId,
                    OrderIndex = orderIndex,
                    Score = score
                });
            }
        }

        exam.TotalQuestions = incomingItems.Count;
        exam.UpdatedAt = DateTime.UtcNow;

        await _examRepository.SaveChangesAsync();
    }

    public async Task RemoveQuestionAsync(Guid examId, int examQuestionId)
    {
        var examQuestion = await _examRepository.GetExamQuestionByIdAsync(examQuestionId);
        if (examQuestion == null || examQuestion.ExamId != examId)
        {
            throw new KeyNotFoundException("Câu hỏi không thuộc đề thi này");
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
                throw new KeyNotFoundException($"Câu hỏi với id '{item.ExamQuestionId}' không tồn tại");
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
            throw new KeyNotFoundException($"Đề thi với id ' {examId} ' không tồn tại");
        }

        var hasPermission = await _examRepository.HasUserPermissionAsync(dto.PublishedByUserId, "EXAM_PUBLISH");
        if (!hasPermission)
        {
            throw new UnauthorizedAccessException("Người dùng không có quyền EXAM_PUBLISH");
        }

        exam.Status = 1;
        exam.UpdatedAt = DateTime.UtcNow;

        await _examRepository.UpdateAsync(exam);
    }

    public async Task AssignAsync(Guid examId, ExamAssignmentCreateDto dto)
    {
        if (!dto.UserId.HasValue && !dto.GroupId.HasValue)
        {
            throw new InvalidOperationException("Phải chỉ định người dùng hoặc nhóm");
        }

        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Đề thi với id ' {examId} ' không tồn tại");
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
