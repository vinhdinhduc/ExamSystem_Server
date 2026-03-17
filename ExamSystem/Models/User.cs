using System;
using System.Collections.Generic;

namespace ExamSystem.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Question> QuestionsCreated { get; set; } = new List<Question>();
    public ICollection<Exam> ExamsCreated { get; set; } = new List<Exam>();
    public ICollection<Group> GroupsCreated { get; set; } = new List<Group>();
    public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    public ICollection<ExamAssignment> ExamAssignments { get; set; } = new List<ExamAssignment>();
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
