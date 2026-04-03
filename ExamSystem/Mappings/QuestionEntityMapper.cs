using ExamSystem.DTOs;
using ExamSystem.Models;

namespace ExamSystem.Mappings;

/// <summary>
/// Map Question entity → QuestionDto (gồm Answers đã sắp xếp). Dùng chung cho QuestionService và AutoMapper.
/// </summary>
public static class QuestionEntityMapper
{
    public static QuestionDto ToDto(Question q)
    {
        var answerDtos = (q.Answers ?? Array.Empty<Answer>())
            .OrderBy(a => a.OrderIndex)
            .Select(a => new AnswerDto(
                a.Id,
                a.QuestionId,
                a.Content,
                a.ImageUrl,
                a.IsCorrect,
                a.OrderIndex))
            .ToList();

        return new QuestionDto(
            q.Id,
            q.SubjectId,
            q.CreatedByUserId,
            q.Content,
            q.ImageUrl,
            q.QuestionType,
            q.DifficultyLevel,
            q.Tags,
            q.Explanation,
            q.IsActive,
            q.CreatedAt,
            q.UpdatedAt,
            answerDtos);
    }
}
