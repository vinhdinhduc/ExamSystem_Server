using System;
using System.Collections.Generic;

namespace ExamSystem.Models;

public class Exam
{
    public Guid Id { get; set; }
    public int SubjectId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int Duration { get; set; }
    public int TotalQuestions { get; set; }
    public decimal PassScore { get; set; }
    public int MaxAttempts { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public bool ShowResultAfter { get; set; }
    public bool ShowCorrectAnswer { get; set; }
    public byte Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AccessCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Subject Subject { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamAssignment> ExamAssignments { get; set; } = new List<ExamAssignment>();
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
}
