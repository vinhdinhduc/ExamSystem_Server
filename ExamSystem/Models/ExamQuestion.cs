using System;

namespace ExamSystem.Models;

public class ExamQuestion
{
    public int Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid QuestionId { get; set; }
    public int OrderIndex { get; set; }
    public decimal Score { get; set; }

    public Exam Exam { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
