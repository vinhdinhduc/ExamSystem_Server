using System;
using System.Collections.Generic;

namespace ExamSystem.Models;

public class SessionAnswer
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid QuestionId { get; set; }
    public string CorrectAnswerIds { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
    public decimal Score { get; set; }
    public DateTime? AnsweredAt { get; set; }

    public ExamSession Session { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public ICollection<SessionAnswerDetail> SessionAnswerDetails { get; set; } = new List<SessionAnswerDetail>();
}
