using Asp.Versioning;
using ExamSystem.Authorization;
using ExamSystem.Common;
using ExamSystem.DTOs;
using ExamSystem.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ExamSystem.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exam-authoring")]
public class ExamAuthoringController : ControllerBase
{
    private readonly IExamAuthoringService _examAuthoringService;
    private readonly IValidator<GenerateExamWithGeminiRequestDto> _generateValidator;
    private readonly IValidator<ImportExamFromFileRequestDto> _importValidator;
    private readonly IValidator<SaveExamDraftRequestDto> _saveDraftValidator;

    public ExamAuthoringController(
        IExamAuthoringService examAuthoringService,
        IValidator<GenerateExamWithGeminiRequestDto> generateValidator,
        IValidator<ImportExamFromFileRequestDto> importValidator,
        IValidator<SaveExamDraftRequestDto> saveDraftValidator)
    {
        _examAuthoringService = examAuthoringService;
        _generateValidator = generateValidator;
        _importValidator = importValidator;
        _saveDraftValidator = saveDraftValidator;
    }

    [HttpPost("generate")]
    [RequirePermission(Permissions.ExamCreate)]
    public async Task<IActionResult> GenerateWithGemini([FromBody] GenerateExamWithGeminiRequestDto dto)
    {
        var validationResult = await _generateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError
                {
                    Details = validationResult.Errors.Select(e => new ValidationDetail
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    }).ToList()
                },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var result = await _examAuthoringService.GenerateWithGeminiAsync(dto);
        return Ok(ApiResponse<ExamAuthoringResultDto>.Success(
            result,
            dto.SaveToDatabase
                ? "Tạo đề thi từ Gemini và lưu dữ liệu thành công"
                : "Sinh nháp đề thi từ Gemini thành công"
        ));
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequirePermission(Permissions.ExamCreate)]
    public async Task<IActionResult> Import([FromForm] ImportExamFromFileRequestDto dto)
    {
        var validationResult = await _importValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError
                {
                    Details = validationResult.Errors.Select(e => new ValidationDetail
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    }).ToList()
                },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var result = await _examAuthoringService.ImportFromFileAsync(dto);
        return Ok(ApiResponse<ExamAuthoringResultDto>.Success(
            result,
            dto.SaveToDatabase
                ? "Import đề thi và lưu dữ liệu thành công"
                : "Import nháp đề thi thành công"
        ));
    }

    [HttpGet("import-template")]
    [RequirePermission(Permissions.ExamCreate)]
    public IActionResult DownloadImportTemplate()
    {
        var csv = string.Join('\n',
        [
            "questionContent,optionA,optionB,optionC,optionD,correctOption,explanation,difficultyLevel,score,questionType,orderIndex",
            "\"SQL Server dùng ngôn ngữ nào để truy vấn dữ liệu?\",\"C#\",\"T-SQL\",\"Java\",\"Python\",\"B\",\"T-SQL là ngôn ngữ truy vấn của SQL Server\",1,1,0,1",
            "\"Mệnh đề dùng để lọc dữ liệu trong câu lệnh SELECT là gì?\",\"ORDER BY\",\"GROUP BY\",\"WHERE\",\"HAVING\",\"C\",\"WHERE dùng để lọc bản ghi theo điều kiện\",1,1,0,2"
        ]);

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", "exam-import-template.csv");
    }

    [HttpPost("save-draft")]
    [RequirePermission(Permissions.ExamCreate)]
    public async Task<IActionResult> SaveDraft([FromBody] SaveExamDraftRequestDto dto)
    {
        var validationResult = await _saveDraftValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError
                {
                    Details = validationResult.Errors.Select(e => new ValidationDetail
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    }).ToList()
                },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var result = await _examAuthoringService.SaveDraftAsync(dto);
        return Ok(ApiResponse<ExamAuthoringResultDto>.Success(
            result,
            "Lưu đề thi từ nháp đã chỉnh sửa thành công"
        ));
    }
}
