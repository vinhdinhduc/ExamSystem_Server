using System;
using System.Collections.Generic;

namespace ExamSystem.Models;

public class ExamSession
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public byte Status { get; set; }
    public decimal? Score { get; set; }
    public bool? IsPassed { get; set; }
    public int? TotalCorrect { get; set; }
    public int AttemptNumber { get; set; }
    public string? IpAddress { get; set; }
    public string? QuestionOrder { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public int ViolationCount { get; set; }
    public DateTime? LastSavedAt { get; set; }

    /// <summary>JSON snapshot câu hỏi + thứ tự đáp án đã gửi lúc bắt đầu (để resume sau reload).</summary>
    public string? SessionResponseSnapshotJson { get; set; }

    /// <summary>Lý do tạm dừng do sự cố (NETWORK_LOSS, PAGE_RELOAD, HEARTBEAT_TIMEOUT, CRASH_OR_UNKNOWN).</summary>
    public string? SystemPauseReason { get; set; }

    public DateTime? SystemPausedAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }

    public Exam Exam { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<SessionAnswer> SessionAnswers { get; set; } = new List<SessionAnswer>();
}
