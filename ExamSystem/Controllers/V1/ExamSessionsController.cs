using Asp.Versioning;
using ExamSystem.Authorization;
using ExamSystem.Common;
using ExamSystem.DTOs;
using ExamSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamSystem.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exam-sessions")]
public class ExamSessionsController : ControllerBase
{
    private readonly IExamSessionService _examSessionService;

    public ExamSessionsController(IExamSessionService examSessionService)
    {
        _examSessionService = examSessionService;
    }

    [HttpPost("{examId:guid}/start")]
    public async Task<IActionResult> StartExam(Guid examId, [FromBody] StartExamRequestDto? dto)
    {
        var userId = ResolveUserId(dto?.UserId);

        var result = await _examSessionService.StartExamAsync(
            examId,
            new StartExamRequestDto(userId, dto?.AccessCode),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(ApiResponse<StartExamResponseDto>.Success(
            result,
            "Bắt đầu bài thi thành công"
        ));
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartExamCompat([FromQuery] Guid examId, [FromBody] StartExamRequestDto? dto)
    {
        if (examId == Guid.Empty)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError
                {
                    Details = new List<ValidationDetail>
                    {
                        new() { Field = "examId", Message = "examId là bắt buộc" }
                    }
                },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var userId = ResolveUserId(dto?.UserId);

        var result = await _examSessionService.StartExamAsync(
            examId,
            new StartExamRequestDto(userId, dto?.AccessCode),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(ApiResponse<StartExamResponseDto>.Success(
            result,
            "Bắt đầu bài thi thành công"
        ));
    }

    [HttpPut("{sessionId:guid}/autosave")]
    public async Task<IActionResult> AutoSave(Guid sessionId, [FromBody] AutoSaveAnswerDto dto)
    {
        var userId = ResolveUserId(dto.UserId);

        await _examSessionService.AutoSaveAnswerAsync(
            sessionId,
            dto with { UserId = userId });

        return Ok(ApiResponse<object>.Success(
            null,
            "Lưu tạm đáp án thành công"
        ));
    }

    [HttpPost("{sessionId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid sessionId, [FromBody] SubmitExamDto? dto)
    {
        var userId = ResolveUserId(dto?.UserId);

        var result = await _examSessionService.SubmitAsync(
            sessionId,
            new SubmitExamDto(userId));

        return Ok(ApiResponse<SubmitExamResultDto>.Success(
            result,
            "Nộp bài thành công"
        ));
    }

    [HttpGet("{sessionId:guid}/review")]
    public async Task<IActionResult> Review(Guid sessionId)
    {
        var userId = ResolveUserId(null);

        var result = await _examSessionService.GetReviewAsync(sessionId, userId);
        return Ok(ApiResponse<ExamSessionReviewDto>.Success(
            result,
            "Lấy kết quả bài làm thành công"
        ));
    }

    [HttpGet("my-results")]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = ResolveUserId(null);
        var results = await _examSessionService.GetStudentResultsAsync(userId);
        return Ok(ApiResponse<List<StudentExamResultItemDto>>.Success(
            results,
            "Lấy danh sách kết quả đã làm thành công"
        ));
    }

    [HttpGet("teacher/assigned-results")]
    [RequirePermission(Permissions.ExamSessionView)]
    public async Task<IActionResult> GetTeacherAssignedResults([FromQuery] Guid? examId)
    {
        var teacherId = ResolveUserId(null);
        var results = await _examSessionService.GetTeacherAssignedResultsAsync(teacherId, examId);

        return Ok(ApiResponse<List<TeacherAssignedExamResultDto>>.Success(
            results,
            "Lấy danh sách kết quả học sinh theo đề thi thành công"
        ));
    }

    private Guid ResolveUserId(Guid? requestUserId)
    {
        if (requestUserId.HasValue && requestUserId.Value != Guid.Empty)
        {
            return requestUserId.Value;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var tokenUserId))
        {
            return tokenUserId;
        }

        throw new UnauthorizedAccessException("Không xác định được người dùng từ token");
    }
}
