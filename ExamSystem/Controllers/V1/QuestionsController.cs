using Asp.Versioning;
using ExamSystem.Authorization;
using ExamSystem.Common;
using ExamSystem.DTOs;
using ExamSystem.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IValidator<QuestionCreateDto> _createValidator;

    public QuestionsController(
        IQuestionService questionService,
        IValidator<QuestionCreateDto> createValidator)
    {
        _questionService = questionService;
        _createValidator = createValidator;
    }

    [HttpGet]
    [RequirePermission(Permissions.QuestionView)]
    public async Task<IActionResult> GetAll([FromQuery] int? subjectId)
    {
        var questions = await _questionService.GetAsync(subjectId);
        return Ok(ApiResponse<List<QuestionDto>>.Success(
            questions,
            "Lấy danh sách câu hỏi thành công"
        ));
    }

    [HttpPost]
    [RequirePermission(Permissions.QuestionCreate)]
    public async Task<IActionResult> Create([FromBody] QuestionCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
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

        var question = await _questionService.CreateAsync(dto);
        return Ok(ApiResponse<QuestionDto>.Success(
            question,
            "Tạo câu hỏi thành công"
        ));
    }
}
