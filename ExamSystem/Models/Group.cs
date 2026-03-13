using System;
using System.Collections.Generic;

namespace ExamSystem.Models;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User CreatedByUser { get; set; } = null!;
    public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    public ICollection<ExamAssignment> ExamAssignments { get; set; } = new List<ExamAssignment>();
}
