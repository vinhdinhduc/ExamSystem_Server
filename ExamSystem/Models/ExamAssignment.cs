using System;

namespace ExamSystem.Models;

public class ExamAssignment
{
    public int Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid? UserId { get; set; }
    public int? GroupId { get; set; }
    public DateTime AssignedAt { get; set; }

    public Exam Exam { get; set; } = null!;
    public User? User { get; set; }
    public Group? Group { get; set; }
}
