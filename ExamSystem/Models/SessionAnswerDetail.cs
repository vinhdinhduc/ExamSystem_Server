using System;

namespace ExamSystem.Models;

public class SessionAnswerDetail
{
    public int Id { get; set; }
    public int SessionAnswerId { get; set; }
    public int AnswerId { get; set; }
    public DateTime? SelectedAt { get; set; }

    public SessionAnswer SessionAnswer { get; set; } = null!;
    public Answer Answer { get; set; } = null!;
}
