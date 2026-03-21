using System.Text.Json;
using ExamSystem.Data;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Services;

public class ExamSessionService : IExamSessionService
{
    private readonly ExamSystemDbContext _context;
    private readonly IExamSessionRepository _examSessionRepository;
    private readonly ILogger<ExamSessionService> _logger;

    public ExamSessionService(ExamSystemDbContext context, IExamSessionRepository examSessionRepository, ILogger<ExamSessionService> logger)
    {
        _context = context;
        _examSessionRepository = examSessionRepository;
        _logger = logger;
    }

    public async Task<StartExamResponseDto> StartExamAsync(Guid examId, StartExamRequestDto dto, string? ipAddress)
    {
        var exam = await _examSessionRepository.GetExamForStartAsync(examId);
        _logger.LogInformation("User {UserId} is attempting to start exam {ExamId} from IP {IpAddress}", dto.UserId, examId, ipAddress);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đề thi với id '{examId}'");
        }

        if (exam.Status != 1)
        {
            throw new InvalidOperationException("Đề thi chưa được công bố");
        }

        var now = DateTime.UtcNow;
        var startUtc = NormalizeToUtc(exam.StartDate);
        var endUtc = NormalizeToUtc(exam.EndDate);

        _logger.LogInformation(
            "Current UTC time is {NowUtc}, exam start raw {StartDateRaw}, start utc {StartDateUtc}, exam end raw {EndDateRaw}, end utc {EndDateUtc}",
            now,
            exam.StartDate,
            startUtc,
            exam.EndDate,
            endUtc);

        if (startUtc.HasValue && now < startUtc.Value)
        {
            throw new InvalidOperationException("Thời gian bắt đầu chưa đến");
        }

        if (endUtc.HasValue && now > endUtc.Value)
        {
            throw new InvalidOperationException("Thời gian kết thúc đã qua");
        }

        var assignedExamIds = await _examSessionRepository.GetAssignedExamIdsAsync(dto.UserId);
        var hasAssignment = assignedExamIds.Contains(examId);
        var canAccessByCode = !string.IsNullOrWhiteSpace(exam.AccessCode) && exam.AccessCode == dto.AccessCode;

        if (!hasAssignment && !canAccessByCode)
        {
            throw new UnauthorizedAccessException("Đề thi chưa được phân công cho người dùng này");
        }

        var attemptCount = await _examSessionRepository.CountAttemptsAsync(examId, dto.UserId);
        if (exam.MaxAttempts > 0 && attemptCount >= exam.MaxAttempts)
        {
            throw new InvalidOperationException("Bạn đã vượt quá số lần làm bài cho phép");
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
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{sessionId}'");
        }

        if (session.UserId != dto.UserId)
        {
            throw new UnauthorizedAccessException("Không thể chỉnh sửa phiên thi của người dùng khác");
        }

        if (session.Status != 0)
        {
            throw new InvalidOperationException("Phiên thi đã được nộp");
        }

        if (DateTime.UtcNow > session.ExpiresAt)
        {
            await GradeAndSubmitAsync(session, true);
            throw new InvalidOperationException("Phiên thi đã hết giờ và được tự động nộp");
        }

        var sessionAnswer = session.SessionAnswers.FirstOrDefault(a => a.QuestionId == dto.QuestionId);
        if (sessionAnswer == null)
        {
            throw new KeyNotFoundException("Câu hỏi không thuộc phiên thi này");
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
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{sessionId}'");
        }

        if (session.UserId != dto.UserId)
        {
            throw new UnauthorizedAccessException("Không thể nộp bài cho phiên thi của người dùng khác");
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

    public async Task<ExamSessionReviewDto> GetReviewAsync(Guid sessionId, Guid userId)
    {
        var session = await _examSessionRepository.GetSessionForReviewAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{sessionId}'");
        }

        var isOwner = session.UserId == userId;
        var isExamCreator = session.Exam.CreatedByUserId == userId;

        if (!isOwner && !isExamCreator)
        {
            throw new UnauthorizedAccessException("Không thể xem kết quả phiên thi của người dùng khác");
        }

        if (!session.SubmittedAt.HasValue)
        {
            throw new InvalidOperationException("Phiên thi chưa được nộp nên chưa thể xem kết quả");
        }

        var examQuestionMap = session.Exam.ExamQuestions.ToDictionary(x => x.QuestionId, x => x);
        var showCorrectAnswer = session.Exam.ShowCorrectAnswer;

        var reviewQuestions = session.SessionAnswers
            .OrderBy(sa => examQuestionMap.TryGetValue(sa.QuestionId, out var eq) ? eq.OrderIndex : int.MaxValue)
            .Select(sa =>
            {
                var examQuestion = examQuestionMap[sa.QuestionId];
                var question = examQuestion.Question;

                var selectedIds = sa.SessionAnswerDetails
                    .Select(d => d.AnswerId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                var correctIds = DeserializeIds(sa.CorrectAnswerIds);
                var correctIdsForResponse = showCorrectAnswer ? correctIds : new List<int>();

                var options = question.Answers
                    .OrderBy(a => a.OrderIndex)
                    .Select(a => new ExamSessionReviewOptionDto(
                        a.Id,
                        a.Content,
                        a.ImageUrl,
                        a.OrderIndex,
                        selectedIds.Contains(a.Id),
                        showCorrectAnswer ? a.IsCorrect : null))
                    .ToList();

                return new ExamSessionReviewQuestionDto(
                    sa.QuestionId,
                    question.Content,
                    question.Explanation,
                    examQuestion.OrderIndex,
                    sa.Score,
                    sa.IsCorrect,
                    selectedIds,
                    correctIdsForResponse,
                    options);
            })
            .ToList();

        return new ExamSessionReviewDto(
            session.Id,
            session.ExamId,
            session.UserId,
            session.Exam.Title,
            session.Score ?? 0,
            session.IsPassed ?? false,
            session.TotalCorrect ?? 0,
            session.StartedAt,
            session.SubmittedAt.Value,
            reviewQuestions);
    }

    public async Task<List<StudentExamResultItemDto>> GetStudentResultsAsync(Guid userId)
    {
        return await _context.ExamSessions
            .AsNoTracking()
            .Include(s => s.Exam)
            .Where(s => s.UserId == userId && s.SubmittedAt.HasValue)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new StudentExamResultItemDto(
                s.Id,
                s.ExamId,
                s.Exam.Title,
                s.Score ?? 0,
                s.IsPassed ?? false,
                s.TotalCorrect ?? 0,
                s.SubmittedAt!.Value,
                s.Status,
                s.AttemptNumber))
            .ToListAsync();
    }

    public async Task<List<TeacherAssignedExamResultDto>> GetTeacherAssignedResultsAsync(Guid teacherId, Guid? examId = null)
    {
        var examsQuery = _context.Exams
            .AsNoTracking()
            .Where(e => e.CreatedByUserId == teacherId);

        if (examId.HasValue)
        {
            examsQuery = examsQuery.Where(e => e.Id == examId.Value);
        }

        var exams = await examsQuery
            .Select(e => new { e.Id, e.Title })
            .ToListAsync();

        if (exams.Count == 0)
        {
            return new List<TeacherAssignedExamResultDto>();
        }

        var examIds = exams.Select(e => e.Id).ToList();

        var directAssignments = await _context.ExamAssignments
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId) && a.UserId.HasValue)
            .Select(a => new { a.ExamId, UserId = a.UserId!.Value })
            .ToListAsync();

        var groupAssignments = await _context.ExamAssignments
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId) && a.GroupId.HasValue)
            .Select(a => new { a.ExamId, GroupId = a.GroupId!.Value })
            .ToListAsync();

        var groupIds = groupAssignments.Select(a => a.GroupId).Distinct().ToList();

        var groupMembers = groupIds.Count == 0
            ? new List<(int GroupId, Guid UserId)>()
            : await _context.GroupMembers
                .AsNoTracking()
                .Where(gm => groupIds.Contains(gm.GroupId))
                .Select(gm => new ValueTuple<int, Guid>(gm.GroupId, gm.UserId))
                .ToListAsync();

        var userIdsByExam = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var exam in exams)
        {
            userIdsByExam[exam.Id] = new HashSet<Guid>();
        }

        foreach (var item in directAssignments)
        {
            userIdsByExam[item.ExamId].Add(item.UserId);
        }

        foreach (var item in groupAssignments)
        {
            var members = groupMembers.Where(m => m.Item1 == item.GroupId).Select(m => m.Item2);
            foreach (var userId in members)
            {
                userIdsByExam[item.ExamId].Add(userId);
            }
        }

        var allAssignedUserIds = userIdsByExam.SelectMany(x => x.Value).Distinct().ToList();

        var users = allAssignedUserIds.Count == 0
            ? new List<User>()
            : await _context.Users
                .AsNoTracking()
                .Where(u => allAssignedUserIds.Contains(u.Id))
                .ToListAsync();

        var submittedSessions = await _context.ExamSessions
            .AsNoTracking()
            .Where(s => examIds.Contains(s.ExamId) && allAssignedUserIds.Contains(s.UserId) && s.SubmittedAt.HasValue)
            .ToListAsync();

        var result = new List<TeacherAssignedExamResultDto>();

        foreach (var exam in exams)
        {
            var assignedUserIds = userIdsByExam[exam.Id].ToList();
            var students = new List<TeacherAssignedStudentResultDto>();

            foreach (var uid in assignedUserIds)
            {
                var user = users.FirstOrDefault(u => u.Id == uid);
                if (user == null) continue;

                var latestSession = submittedSessions
                    .Where(s => s.ExamId == exam.Id && s.UserId == uid)
                    .OrderByDescending(s => s.SubmittedAt)
                    .ThenByDescending(s => s.AttemptNumber)
                    .FirstOrDefault();

                students.Add(new TeacherAssignedStudentResultDto(
                    user.Id,
                    user.FullName,
                    user.Email,
                    latestSession != null,
                    latestSession?.Id,
                    latestSession?.Score,
                    latestSession?.IsPassed,
                    latestSession?.SubmittedAt,
                    submittedSessions.Count(s => s.ExamId == exam.Id && s.UserId == uid)
                ));
            }

            var totalAssigned = students.Count;
            var totalSubmitted = students.Count(s => s.IsSubmitted);

            result.Add(new TeacherAssignedExamResultDto(
                exam.Id,
                exam.Title,
                totalAssigned,
                totalSubmitted,
                totalAssigned - totalSubmitted,
                students.OrderBy(s => s.FullName).ToList()
            ));
        }

        return result;
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

    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var dateTime = value.Value;
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }

        if (dateTime.Kind == DateTimeKind.Local)
        {
            return dateTime.ToUniversalTime();
        }

        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        var timezone = ResolveVietnamTimeZone();
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timezone);
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
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
