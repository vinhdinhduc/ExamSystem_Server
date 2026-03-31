using Asp.Versioning;
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
[Route("api/v{version:apiVersion}/exam")]
public class ExamController : ControllerBase
{
    private readonly IExamSessionService _examSessionService;

    public ExamController(IExamSessionService examSessionService)
    {
        _examSessionService = examSessionService;
    }

    [HttpPost("save-progress")]
    public async Task<IActionResult> SaveProgress([FromBody] SaveExamProgressRequestDto dto)
    {
        var userId = ResolveUserId(dto.UserId);

        var result = await _examSessionService.SaveProgressAsync(
            dto.SessionId,
            new SaveExamProgressDto(
                userId,
                dto.QuestionId,
                dto.AnswerIds,
                dto.CurrentQuestionIndex));

        return Ok(ApiResponse<SaveExamProgressResultDto>.Success(
            result,
            result.IsAutoSubmitted
                ? "Phiên thi đã quá hạn và được tự động nộp"
                : "Lưu tiến độ làm bài thành công"
        ));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitExamBySessionDto dto)
    {
        var userId = ResolveUserId(dto.UserId);
        var result = await _examSessionService.SubmitAsync(dto.SessionId, new SubmitExamDto(userId));

        return Ok(ApiResponse<SubmitExamResultDto>.Success(
            result,
            "Nộp bài thành công"
        ));
    }

    [HttpPost("violation")]
    public async Task<IActionResult> ReportViolation([FromBody] ExamViolationDto dto)
    {
        var userId = ResolveUserId(dto.UserId);
        var result = await _examSessionService.ReportViolationAsync(dto with { UserId = userId });

        return Ok(ApiResponse<ExamViolationResultDto>.Success(
            result,
            result.IsForceSubmitted
                ? "Đã ghi nhận vi phạm. Phiên thi bị tự động nộp do vượt ngưỡng vi phạm"
                : "Đã ghi nhận vi phạm"
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
