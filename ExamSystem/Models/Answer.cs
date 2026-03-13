using System.Collections.Generic;

namespace ExamSystem.Models;

public class Answer
{
    public int Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }

    public Question Question { get; set; } = null!;
    public ICollection<SessionAnswerDetail> SessionAnswerDetails { get; set; } = new List<SessionAnswerDetail>();
}
