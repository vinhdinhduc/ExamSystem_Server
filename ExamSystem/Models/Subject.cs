using System;
using System.Collections.Generic;

namespace ExamSystem.Models;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}
