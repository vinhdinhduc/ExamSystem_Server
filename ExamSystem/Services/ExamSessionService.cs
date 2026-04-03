using System.Text.Json;
using ExamSystem.Data;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Realtime;
using ExamSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace ExamSystem.Services;

public class ExamSessionService : IExamSessionService
{
    private const byte SessionStatusInProgress = 0;
    private const byte SessionStatusSubmitted = 1;
    private const byte SessionStatusTimedOut = 2;
    private const byte SessionStatusForceSubmitted = 3;
    private const byte SessionStatusPausedSystem = 4;
    private const byte SessionStatusDisqualifiedByAdmin = 6;

    private readonly ExamSystemDbContext _context;
    private readonly IExamSessionRepository _examSessionRepository;
    private readonly ILogger<ExamSessionService> _logger;
    private readonly IHubContext<ExamMonitoringHub> _hubContext;

    public ExamSessionService(
        ExamSystemDbContext context,
        IExamSessionRepository examSessionRepository,
        ILogger<ExamSessionService> logger,
        IHubContext<ExamMonitoringHub> hubContext)
    {
        _context = context;
        _examSessionRepository = examSessionRepository;
        _logger = logger;
        _hubContext = hubContext;
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

        var existingActive = await _examSessionRepository.GetActiveSessionForUserExamAsync(examId, dto.UserId, now);
        if (existingActive != null)
        {
            return await BuildResumeStartResponseFromSessionAsync(existingActive);
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
            Status = SessionStatusInProgress,
            AttemptNumber = attemptCount + 1,
            IpAddress = ipAddress,
            QuestionOrder = JsonSerializer.Serialize(orderedQuestions.Select(q => q.QuestionId).ToList()),
            CurrentQuestionIndex = 0,
            ViolationCount = 0,
            LastSavedAt = now,
            LastHeartbeatAt = now
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

        session.SessionResponseSnapshotJson = JsonSerializer.Serialize(responseQuestions);

        await _examSessionRepository.AddSessionAsync(session);
        await _examSessionRepository.SaveChangesAsync();

        return new StartExamResponseDto(
            session.Id,
            session.StartedAt,
            session.ExpiresAt,
            session.AttemptNumber,
            responseQuestions);
    }

    private Task<StartExamResponseDto> BuildResumeStartResponseFromSessionAsync(ExamSession session)
    {
        var questions = DeserializeSnapshotQuestionList(session);
        return Task.FromResult(new StartExamResponseDto(
            session.Id,
            session.StartedAt,
            session.ExpiresAt,
            session.AttemptNumber,
            questions));
    }

    private static List<ExamSessionQuestionDto> DeserializeSnapshotQuestionList(ExamSession session)
    {
        if (!string.IsNullOrWhiteSpace(session.SessionResponseSnapshotJson))
        {
            var list = JsonSerializer.Deserialize<List<ExamSessionQuestionDto>>(session.SessionResponseSnapshotJson);
            if (list is { Count: > 0 })
            {
                return list;
            }
        }

        return BuildFallbackQuestionListFromOrder(session);
    }

    private static List<ExamSessionQuestionDto> BuildFallbackQuestionListFromOrder(ExamSession session)
    {
        var exam = session.Exam;
        var orderIds = JsonSerializer.Deserialize<List<Guid>>(session.QuestionOrder ?? "[]") ?? new List<Guid>();
        var examQMap = exam.ExamQuestions.ToDictionary(x => x.QuestionId, x => x);
        var list = new List<ExamSessionQuestionDto>();

        foreach (var qid in orderIds)
        {
            if (!examQMap.TryGetValue(qid, out var eq))
            {
                continue;
            }

            var question = eq.Question;
            var answerIds = question.Answers.OrderBy(a => a.OrderIndex).Select(a => a.Id).ToList();
            list.Add(new ExamSessionQuestionDto(qid, eq.OrderIndex, answerIds));
        }

        return list;
    }

    public async Task AutoSaveAnswerAsync(Guid sessionId, AutoSaveAnswerDto dto)
    {
        await SaveProgressCoreAsync(
            sessionId,
            dto.UserId,
            dto.QuestionId,
            dto.AnswerIds,
            null,
            emitRealtime: true);
    }

    public async Task<SaveExamProgressResultDto> SaveProgressAsync(Guid sessionId, SaveExamProgressDto dto)
    {
        var result = await SaveProgressCoreAsync(
            sessionId,
            dto.UserId,
            dto.QuestionId,
            dto.AnswerIds,
            dto.CurrentQuestionIndex,
            emitRealtime: true);

        return new SaveExamProgressResultDto(
            result.SessionId,
            result.CurrentQuestionIndex,
            result.ViolationCount,
            result.LastSavedAt,
            result.Status,
            result.IsAutoSubmitted);
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

        if (session.Status == SessionStatusPausedSystem)
        {
            throw new InvalidOperationException("Phiên thi đang tạm dừng chờ quản trị viên, không thể tự nộp bài");
        }

        if (session.Status != SessionStatusInProgress)
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
        await GradeAndSubmitAsync(session, timedOut, false);

        return new SubmitExamResultDto(
            session.Id,
            session.Score ?? 0,
            session.IsPassed ?? false,
            session.TotalCorrect ?? 0,
            session.SubmittedAt ?? DateTime.UtcNow,
            session.Status);
    }

    public async Task<ExamViolationResultDto> ReportViolationAsync(ExamViolationDto dto)
    {
        var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TAB_SWITCH",
            "COPY",
            "PASTE",
            "EXIT_FULLSCREEN",
            "DEVTOOLS"
        };

        if (!validTypes.Contains(dto.Type))
        {
            throw new InvalidOperationException("Loại vi phạm không hợp lệ");
        }

        var session = await _examSessionRepository.GetSessionByIdAsync(dto.SessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{dto.SessionId}'");
        }

        if (session.UserId != dto.UserId)
        {
            throw new UnauthorizedAccessException("Không thể báo vi phạm cho phiên thi của người dùng khác");
        }

        // Chỉ ghi nhận vi phạm khi đang làm bài. Tạm dừng sự cố chờ admin / đã nộp — không tăng ViolationCount (không coi mất mạng như gian lận).
        if (session.Status != SessionStatusInProgress)
        {
            return new ExamViolationResultDto(
                session.Id,
                session.ViolationCount,
                session.Status == SessionStatusForceSubmitted,
                session.Status,
                session.SubmittedAt);
        }

        session.ViolationCount += 1;
        if (dto.CurrentQuestionIndex.HasValue && dto.CurrentQuestionIndex.Value >= 0)
        {
            session.CurrentQuestionIndex = dto.CurrentQuestionIndex.Value;
        }

        var now = DateTime.UtcNow;
        session.LastSavedAt = now;

        var isForceSubmitted = session.ViolationCount >= 3;
        if (isForceSubmitted)
        {
            await GradeAndSubmitAsync(session, timedOut: false, forced: true);
        }
        else
        {
            await _examSessionRepository.SaveChangesAsync();
        }

        await EmitStudentProgressAsync(session, "violation");

        return new ExamViolationResultDto(
            session.Id,
            session.ViolationCount,
            isForceSubmitted,
            session.Status,
            session.SubmittedAt);
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
            .Select(e => new { e.Id, e.Title, e.EndDate })
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

        var allSessions = allAssignedUserIds.Count == 0
            ? new List<ExamSession>()
            : await _examSessionRepository.GetSessionsByExamAndUsersAsync(examIds, allAssignedUserIds);

        var submittedSessions = allSessions
            .Where(s => s.SubmittedAt.HasValue)
            .ToList();

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

                var latestAnySession = allSessions
                    .Where(s => s.ExamId == exam.Id && s.UserId == uid)
                    .OrderByDescending(s => s.StartedAt)
                    .ThenByDescending(s => s.AttemptNumber)
                    .FirstOrDefault();

                var learningStatus = ResolveLearningStatus(latestAnySession, latestSession, exam.EndDate);

                students.Add(new TeacherAssignedStudentResultDto(
                    user.Id,
                    user.FullName,
                    user.Email,
                    learningStatus,
                    latestSession != null,
                    latestSession?.Id,
                    latestSession?.Score,
                    latestSession?.IsPassed,
                    latestSession?.SubmittedAt,
                    allSessions.Count(s => s.ExamId == exam.Id && s.UserId == uid)
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

    private async Task<(Guid SessionId, int CurrentQuestionIndex, int ViolationCount, DateTime LastSavedAt, byte Status, bool IsAutoSubmitted)> SaveProgressCoreAsync(
        Guid sessionId,
        Guid userId,
        Guid questionId,
        List<int> answerIds,
        int? currentQuestionIndex,
        bool emitRealtime)
    {
        var session = await _examSessionRepository.GetSessionByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{sessionId}'");
        }

        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException("Không thể chỉnh sửa phiên thi của người dùng khác");
        }

        if (session.Status == SessionStatusPausedSystem)
        {
            throw new InvalidOperationException("Phiên thi đang tạm dừng chờ quản trị viên");
        }

        if (session.Status != SessionStatusInProgress)
        {
            throw new InvalidOperationException("Phiên thi đã được nộp");
        }

        var now = DateTime.UtcNow;
        if (now > session.ExpiresAt)
        {
            await GradeAndSubmitAsync(session, timedOut: true, forced: false);

            return (
                session.Id,
                session.CurrentQuestionIndex,
                session.ViolationCount,
                session.LastSavedAt ?? now,
                session.Status,
                true);
        }

        var sessionAnswer = session.SessionAnswers.FirstOrDefault(a => a.QuestionId == questionId);
        if (sessionAnswer == null)
        {
            throw new KeyNotFoundException("Câu hỏi không thuộc phiên thi này");
        }

        if (sessionAnswer.SessionAnswerDetails.Any())
        {
            await _examSessionRepository.RemoveSessionAnswerDetailsAsync(sessionAnswer.SessionAnswerDetails.ToList());
        }

        foreach (var answerId in answerIds.Distinct())
        {
            await _examSessionRepository.AddSessionAnswerDetailAsync(new SessionAnswerDetail
            {
                SessionAnswerId = sessionAnswer.Id,
                AnswerId = answerId,
                SelectedAt = now
            });
        }

        if (currentQuestionIndex.HasValue && currentQuestionIndex.Value >= 0)
        {
            session.CurrentQuestionIndex = currentQuestionIndex.Value;
        }

        session.LastSavedAt = now;
        sessionAnswer.AnsweredAt = now;
        await _examSessionRepository.SaveChangesAsync();

        if (emitRealtime)
        {
            await EmitStudentProgressAsync(session, "save_progress");
        }

        return (
            session.Id,
            session.CurrentQuestionIndex,
            session.ViolationCount,
            session.LastSavedAt ?? now,
            session.Status,
            false);
    }

    public async Task<int> AutoSubmitExpiredSessionsAsync()
    {
        var sessions = await _examSessionRepository.GetExpiredActiveSessionsAsync(DateTime.UtcNow);
        foreach (var session in sessions)
        {
            await GradeAndSubmitAsync(session, timedOut: true, forced: false);
        }

        return sessions.Count;
    }

    public async Task<int> PauseSessionsWithStaleHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddMinutes(-5);

        var sessions = await _context.ExamSessions
            .Include(s => s.SessionAnswers)
                .ThenInclude(sa => sa.SessionAnswerDetails)
            .Where(s =>
                s.Status == SessionStatusInProgress &&
                s.ExpiresAt > now &&
                s.LastHeartbeatAt != null &&
                s.LastHeartbeatAt < threshold)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return 0;
        }

        foreach (var session in sessions)
        {
            session.Status = SessionStatusPausedSystem;
            session.SystemPauseReason = "HEARTBEAT_TIMEOUT";
            session.SystemPausedAt = now;
            session.LastSavedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var session in sessions)
        {
            await EmitStudentProgressAsync(session, "system_pause");
            _logger.LogWarning(
                "Heartbeat watchdog: phiên {SessionId} tạm dừng do không nhận heartbeat sau 5 phút",
                session.Id);
        }

        return sessions.Count;
    }

    public async Task<SystemInterruptionResultDto> ReportSystemInterruptionAsync(SystemInterruptionReportDto dto)
    {
        var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "NETWORK_LOSS",
            "PAGE_RELOAD",
            "HEARTBEAT_TIMEOUT",
            "CRASH_OR_UNKNOWN"
        };

        if (!validTypes.Contains(dto.Type))
        {
            throw new InvalidOperationException("Loại sự cố không hợp lệ");
        }

        var session = await _examSessionRepository.GetSessionByIdAsync(dto.SessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{dto.SessionId}'");
        }

        if (session.UserId != dto.UserId)
        {
            throw new UnauthorizedAccessException("Không thể báo sự cố cho phiên thi của người dùng khác");
        }

        if (session.Status == SessionStatusPausedSystem)
        {
            return new SystemInterruptionResultDto(
                session.Id,
                session.Status,
                session.SystemPauseReason,
                session.SystemPausedAt);
        }

        if (session.Status != SessionStatusInProgress)
        {
            throw new InvalidOperationException("Phiên thi không còn đang làm dở");
        }

        var now = DateTime.UtcNow;
        if (dto.CurrentQuestionIndex.HasValue && dto.CurrentQuestionIndex.Value >= 0)
        {
            session.CurrentQuestionIndex = dto.CurrentQuestionIndex.Value;
        }

        session.Status = SessionStatusPausedSystem;
        session.SystemPauseReason = dto.Type.ToUpperInvariant();
        session.SystemPausedAt = now;
        session.LastSavedAt = now;

        await _examSessionRepository.SaveChangesAsync();
        await EmitStudentProgressAsync(session, "system_pause");

        return new SystemInterruptionResultDto(
            session.Id,
            session.Status,
            session.SystemPauseReason,
            session.SystemPausedAt);
    }

    public async Task HeartbeatAsync(ExamSessionHeartbeatDto dto)
    {
        var session = await _examSessionRepository.GetSessionByIdAsync(dto.SessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{dto.SessionId}'");
        }

        if (session.UserId != dto.UserId)
        {
            throw new UnauthorizedAccessException("Không thể gửi heartbeat cho phiên thi của người dùng khác");
        }

        if (session.Status != SessionStatusInProgress)
        {
            return;
        }

        var now = DateTime.UtcNow;
        session.LastHeartbeatAt = now;
        await _examSessionRepository.SaveChangesAsync();
    }

    public async Task<SessionRuntimeStatusDto> GetSessionRuntimeStatusAsync(Guid sessionId, Guid userId)
    {
        var session = await _examSessionRepository.GetSessionByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{sessionId}'");
        }

        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException("Không thể xem trạng thái phiên thi của người dùng khác");
        }

        return new SessionRuntimeStatusDto(
            session.Id,
            session.Status,
            session.SystemPauseReason,
            session.SystemPausedAt,
            session.ExpiresAt,
            session.CurrentQuestionIndex,
            session.ViolationCount);
    }

    public async Task<List<PendingSystemPauseItemDto>> GetPendingSystemPausesAsync(Guid viewerUserId, bool viewerIsAdmin)
    {
        var query = _context.ExamSessions
            .AsNoTracking()
            .Include(s => s.Exam)
            .Include(s => s.User)
            .Where(s => s.Status == SessionStatusPausedSystem);

        if (!viewerIsAdmin)
        {
            query = query.Where(s => s.Exam.CreatedByUserId == viewerUserId);
        }

        return await query
            .OrderByDescending(s => s.SystemPausedAt ?? s.StartedAt)
            .Select(s => new PendingSystemPauseItemDto(
                s.Id,
                s.ExamId,
                s.Exam.Title,
                s.UserId,
                s.User.FullName,
                s.User.Email,
                s.SystemPauseReason,
                s.SystemPausedAt,
                s.ExpiresAt,
                s.CurrentQuestionIndex,
                s.ViolationCount))
            .ToListAsync();
    }

    public async Task<AdminResolvePauseResultDto> AdminResolveSystemPauseAsync(
        Guid sessionId,
        AdminResolveSessionPauseRequestDto dto,
        Guid resolverUserId,
        bool resolverIsAdmin)
    {
        var session = await _examSessionRepository.GetSessionByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phiên thi với id '{sessionId}'");
        }

        if (session.Status != SessionStatusPausedSystem)
        {
            throw new InvalidOperationException("Phiên thi không ở trạng thái chờ xử lý sự cố");
        }

        if (!resolverIsAdmin && session.Exam.CreatedByUserId != resolverUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xử lý phiên thi này");
        }

        var decision = (dto.Decision ?? "").Trim().ToUpperInvariant();
        switch (decision)
        {
            case "RESUME":
                var nowResume = DateTime.UtcNow;
                // Bù thời gian làm bài: trong lúc tạm dừng hệ thống, đồng hồ thật vẫn trôi nếu không gia hạn ExpiresAt → client nhận 0s, kẹt 00:00.
                var pauseBegan = session.SystemPausedAt ?? nowResume;
                var pausedFor = nowResume - pauseBegan;
                if (pausedFor > TimeSpan.Zero)
                {
                    session.ExpiresAt = session.ExpiresAt.Add(pausedFor);
                }

                session.Status = SessionStatusInProgress;
                session.SystemPauseReason = null;
                session.SystemPausedAt = null;
                session.LastHeartbeatAt = nowResume;
                await _examSessionRepository.SaveChangesAsync();
                await EmitStudentProgressAsync(session, "admin_resumed");
                return new AdminResolvePauseResultDto(session.Id, session.Status, null);

            case "SUBMIT":
                await GradeAndSubmitAsync(session, timedOut: false, forced: false);
                return new AdminResolvePauseResultDto(session.Id, session.Status, session.SubmittedAt);

            case "DISQUALIFY":
                await GradeAndSubmitDisqualifiedByAdminAsync(session);
                return new AdminResolvePauseResultDto(session.Id, session.Status, session.SubmittedAt);

            default:
                throw new InvalidOperationException("Quyết định không hợp lệ (RESUME, SUBMIT, DISQUALIFY)");
        }
    }

    private async Task GradeAndSubmitDisqualifiedByAdminAsync(ExamSession session)
    {
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
            sessionAnswer.Score = 0;
            sessionAnswer.AnsweredAt ??= DateTime.UtcNow;
        }

        session.SubmittedAt = DateTime.UtcNow;
        session.TotalCorrect = 0;
        session.Score = 0;
        session.IsPassed = false;
        session.Status = SessionStatusDisqualifiedByAdmin;

        await _examSessionRepository.SaveChangesAsync();
        await EmitStudentSubmitAsync(session, "admin_disqualified");
    }

    private async Task GradeAndSubmitAsync(ExamSession session, bool timedOut, bool forced)
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

        var percentScore = total > 0 ? Math.Round((achieved / total) * 10, 2) : 0;

        session.SubmittedAt = DateTime.UtcNow;
        session.TotalCorrect = totalCorrect;
        session.Score = percentScore;
        session.IsPassed = percentScore >= session.Exam.PassScore;
        session.Status = forced
            ? SessionStatusForceSubmitted
            : timedOut
                ? SessionStatusTimedOut
                : SessionStatusSubmitted;

        await _examSessionRepository.SaveChangesAsync();
        await EmitStudentSubmitAsync(session, forced ? "force_submitted" : timedOut ? "timed_out" : "submitted");
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

    private async Task EmitStudentProgressAsync(ExamSession session, string action)
    {
        var remainingSeconds = (int)Math.Max(0, (session.ExpiresAt - DateTime.UtcNow).TotalSeconds);
        var totalQuestions = session.SessionAnswers.Count;
        var answeredQuestions = session.SessionAnswers.Count(sa => sa.SessionAnswerDetails.Any());
        var progressPercent = totalQuestions == 0 ? 0 : Math.Round((double)answeredQuestions / totalQuestions * 100, 2);

        var payload = new
        {
            sessionId = session.Id,
            examId = session.ExamId,
            userId = session.UserId,
            action,
            currentQuestionIndex = session.CurrentQuestionIndex,
            violationCount = session.ViolationCount,
            remainingSeconds,
            progressPercent,
            status = session.Status
        };

        await _hubContext.Clients.Group($"exam:{session.ExamId}").SendAsync("student_progress", payload);
        await _hubContext.Clients.Group("exam-monitoring").SendAsync("student_progress", payload);
    }

    private async Task EmitStudentSubmitAsync(ExamSession session, string reason)
    {
        var payload = new
        {
            sessionId = session.Id,
            examId = session.ExamId,
            userId = session.UserId,
            reason,
            submittedAt = session.SubmittedAt,
            score = session.Score,
            isPassed = session.IsPassed,
            status = session.Status
        };

        await _hubContext.Clients.Group($"exam:{session.ExamId}").SendAsync("student_submit", payload);
        await _hubContext.Clients.Group("exam-monitoring").SendAsync("student_submit", payload);
    }

    private static string ResolveLearningStatus(ExamSession? latestAnySession, ExamSession? latestSubmittedSession, DateTime? examEndDate)
    {
        if (latestSubmittedSession != null)
        {
            return "DA_LAM";
        }

        if (latestAnySession is { Status: SessionStatusPausedSystem } && DateTime.UtcNow <= latestAnySession.ExpiresAt)
        {
            return "CHO_XU_LY_SU_CO";
        }

        if (latestAnySession is { Status: SessionStatusInProgress } && DateTime.UtcNow <= latestAnySession.ExpiresAt)
        {
            return "DANG_LAM";
        }

        var endUtc = NormalizeToUtc(examEndDate);
        if (endUtc.HasValue && DateTime.UtcNow > endUtc.Value)
        {
            return "QUA_HAN";
        }

        return "CHUA_LAM";
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
