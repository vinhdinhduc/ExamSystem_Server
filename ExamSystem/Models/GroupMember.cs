using System;

namespace ExamSystem.Models;

public class GroupMember
{
    public int GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
