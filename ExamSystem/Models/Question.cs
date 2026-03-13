using System;
using System.Collections.Generic;

namespace ExamSystem.Models;

public class Question
{
    public Guid Id { get; set; }
    public int SubjectId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public byte QuestionType { get; set; }
    public byte DifficultyLevel { get; set; }
    public string? Tags { get; set; }
    public string? Explanation { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Subject Subject { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    public ICollection<SessionAnswer> SessionAnswers { get; set; } = new List<SessionAnswer>();
}
