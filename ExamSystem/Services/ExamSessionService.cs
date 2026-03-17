using System.Text.Json;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class ExamSessionService : IExamSessionService
{
    private readonly IExamSessionRepository _examSessionRepository;

    public ExamSessionService(IExamSessionRepository examSessionRepository)
    {
        _examSessionRepository = examSessionRepository;
    }

    public async Task<StartExamResponseDto> StartExamAsync(Guid examId, StartExamRequestDto dto, string? ipAddress)
    {
        var exam = await _examSessionRepository.GetExamForStartAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with id '{examId}' not found");
        }

        if (exam.Status != 1)
        {
            throw new InvalidOperationException("Exam is not published");
        }

        var now = DateTime.UtcNow;
        if (exam.StartDate.HasValue && now < exam.StartDate.Value)
        {
            throw new InvalidOperationException("Exam is not started yet");
        }

        if (exam.EndDate.HasValue && now > exam.EndDate.Value)
        {
            throw new InvalidOperationException("Exam is already closed");
        }

        var assignedExamIds = await _examSessionRepository.GetAssignedExamIdsAsync(dto.UserId);
        var hasAssignment = assignedExamIds.Contains(examId);
        var canAccessByCode = !string.IsNullOrWhiteSpace(exam.AccessCode) && exam.AccessCode == dto.AccessCode;

        if (!hasAssignment && !canAccessByCode)
        {
            throw new UnauthorizedAccessException("Exam is not assigned to this user");
        }

        var attemptCount = await _examSessionRepository.CountAttemptsAsync(examId, dto.UserId);
        if (exam.MaxAttempts > 0 && attemptCount >= exam.MaxAttempts)
        {
            throw new InvalidOperationException("Maximum attempts exceeded");
        }

        var orderedQuestions = exam.ExamQuestions
            .OrderBy(q => q.OrderIndex)
            .ToList();

        if (exam.ShuffleQuestions)
        {
            orderedQuestions = orderedQuestions.OrderBy(_ => Guid.NewGuid()).ToList();
        }

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = examId,
            UserId = dto.UserId,
            StartedAt = now,
            ExpiresAt = now.AddMinutes(exam.Duration),
            Status = 0,
            AttemptNumber = attemptCount + 1,
            IpAddress = ipAddress,
            QuestionOrder = JsonSerializer.Serialize(orderedQuestions.Select(q => q.QuestionId).ToList())
        };

        var responseQuestions = new List<ExamSessionQuestionDto>();

        foreach (var examQuestion in orderedQuestions)
        {
            var correctIds = examQuestion.Question.Answers
                .Where(a => a.IsCorrect)
                .Select(a => a.Id)
                .OrderBy(id => id)
                .ToList();

            session.SessionAnswers.Add(new SessionAnswer
            {
                QuestionId = examQuestion.QuestionId,
                CorrectAnswerIds = JsonSerializer.Serialize(correctIds),
                Score = 0
            });

            var answerIds = examQuestion.Question.Answers
                .Select(a => a.Id)
                .ToList();

            if (exam.ShuffleAnswers)
            {
                answerIds = answerIds.OrderBy(_ => Guid.NewGuid()).ToList();
            }

            responseQuestions.Add(new ExamSessionQuestionDto(
                examQuestion.QuestionId,
                examQuestion.OrderIndex,
                answerIds));
        }

        await _examSessionRepository.AddSessionAsync(session);
        await _examSessionRepository.SaveChangesAsync();

        return new StartExamResponseDto(
            session.Id,
            session.StartedAt,
            session.ExpiresAt,
            session.AttemptNumber,
            responseQuestions);
    }

    public async Task AutoSaveAnswerAsync(Guid sessionId, AutoSaveAnswerDto dto)
    {
        var session = await _examSessionRepository.GetSessionByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Session with id '{sessionId}' not found");
        }

        if (session.UserId != dto.UserId)
        {
            throw new UnauthorizedAccessException("Cannot modify another user session");
        }

        if (session.Status != 0)
        {
            throw new InvalidOperationException("Session already submitted");
        }

        if (DateTime.UtcNow > session.ExpiresAt)
        {
            await GradeAndSubmitAsync(session, true);
            throw new InvalidOperationException("Session expired and was auto submitted");
        }

        var sessionAnswer = session.SessionAnswers.FirstOrDefault(a => a.QuestionId == dto.QuestionId);
        if (sessionAnswer == null)
        {
            throw new KeyNotFoundException("Question is not in this session");
        }

        if (sessionAnswer.SessionAnswerDetails.Any())
        {
            await _examSessionRepository.RemoveSessionAnswerDetailsAsync(sessionAnswer.SessionAnswerDetails.ToList());
        }

        foreach (var answerId in dto.AnswerIds.Distinct())
        {
            await _examSessionRepository.AddSessionAnswerDetailAsync(new SessionAnswerDetail
            {
                SessionAnswerId = sessionAnswer.Id,
                AnswerId = answerId,
                SelectedAt = DateTime.UtcNow
            });
        }

        sessionAnswer.AnsweredAt = DateTime.UtcNow;
        await _examSessionRepository.SaveChangesAsync();
    }

    public async Task<SubmitExamResultDto> SubmitAsync(Guid sessionId, SubmitExamDto dto)
    {
        var session = await _examSessionRepository.GetSessionByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Session with id '{sessionId}' not found");
        }

        if (session.UserId != dto.UserId)
        {
            throw new UnauthorizedAccessException("Cannot submit another user session");
        }

        if (session.Status != 0)
        {
            return new SubmitExamResultDto(
                session.Id,
                session.Score ?? 0,
                session.IsPassed ?? false,
                session.TotalCorrect ?? 0,
                session.SubmittedAt ?? DateTime.UtcNow,
                session.Status);
        }

        var timedOut = DateTime.UtcNow > session.ExpiresAt;
        await GradeAndSubmitAsync(session, timedOut);

        return new SubmitExamResultDto(
            session.Id,
            session.Score ?? 0,
            session.IsPassed ?? false,
            session.TotalCorrect ?? 0,
            session.SubmittedAt ?? DateTime.UtcNow,
            session.Status);
    }

    public async Task<int> AutoSubmitExpiredSessionsAsync()
    {
        var sessions = await _examSessionRepository.GetExpiredActiveSessionsAsync(DateTime.UtcNow);
        foreach (var session in sessions)
        {
            await GradeAndSubmitAsync(session, true);
        }

        return sessions.Count;
    }

    private async Task GradeAndSubmitAsync(ExamSession session, bool timedOut)
    {
        var examQuestionScoreMap = session.Exam.ExamQuestions.ToDictionary(x => x.QuestionId, x => x.Score);

        var totalCorrect = 0;
        decimal achieved = 0;
        decimal total = 0;

        foreach (var questionScore in examQuestionScoreMap.Values)
        {
            total += questionScore;
        }

        foreach (var sessionAnswer in session.SessionAnswers)
        {
            var correctIds = DeserializeIds(sessionAnswer.CorrectAnswerIds);
            var selectedIds = sessionAnswer.SessionAnswerDetails
                .Select(d => d.AnswerId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var isCorrect = correctIds.SequenceEqual(selectedIds);
            sessionAnswer.IsCorrect = isCorrect;
            sessionAnswer.Score = isCorrect && examQuestionScoreMap.TryGetValue(sessionAnswer.QuestionId, out var questionScore)
                ? questionScore
                : 0;
            sessionAnswer.AnsweredAt ??= DateTime.UtcNow;

            if (isCorrect)
            {
                totalCorrect++;
                achieved += sessionAnswer.Score;
            }
        }

        var percentScore = total > 0 ? Math.Round((achieved / total) * 100, 2) : 0;

        session.SubmittedAt = DateTime.UtcNow;
        session.TotalCorrect = totalCorrect;
        session.Score = percentScore;
        session.IsPassed = percentScore >= session.Exam.PassScore;
        session.Status = timedOut ? (byte)2 : (byte)1;

        await _examSessionRepository.SaveChangesAsync();
    }

    private static List<int> DeserializeIds(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<int>();
        }

        return JsonSerializer.Deserialize<List<int>>(json)?.OrderBy(id => id).ToList() ?? new List<int>();
    }
}
