namespace ExamSystem.Models;

public class PasswordResetToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Otp { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
