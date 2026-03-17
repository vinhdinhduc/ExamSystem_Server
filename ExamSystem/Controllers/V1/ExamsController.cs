using Asp.Versioning;
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
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IValidator<ExamCreateDto> _createValidator;
    private readonly IValidator<ExamUpdateDto> _updateValidator;
    private readonly IValidator<ExamQuestionCreateDto> _addQuestionValidator;
    private readonly IValidator<ReorderExamQuestionsDto> _reorderValidator;
    private readonly IValidator<PublishExamDto> _publishValidator;
    private readonly IValidator<ExamAssignmentCreateDto> _assignmentValidator;

    public ExamsController(
        IExamService examService,
        IValidator<ExamCreateDto> createValidator,
        IValidator<ExamUpdateDto> updateValidator,
        IValidator<ExamQuestionCreateDto> addQuestionValidator,
        IValidator<ReorderExamQuestionsDto> reorderValidator,
        IValidator<PublishExamDto> publishValidator,
        IValidator<ExamAssignmentCreateDto> assignmentValidator)
    {
        _examService = examService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addQuestionValidator = addQuestionValidator;
        _reorderValidator = reorderValidator;
        _publishValidator = publishValidator;
        _assignmentValidator = assignmentValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ExamFilterDto filter)
    {
        var (items, total, page, pageSize) = await _examService.GetPagedAsync(filter);
        var pages = (int)Math.Ceiling(total / (double)pageSize);

        var paginatedResult = new PaginatedResult<ExamDto>
        {
            Meta = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                Pages = pages,
                Total = total
            },
            Result = items
        };

        return Ok(ApiResponse<PaginatedResult<ExamDto>>.Success(
            paginatedResult,
            "Exams retrieved successfully"
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var exam = await _examService.GetByIdAsync(id);
        if (exam == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Exam with id '{id}'" },
                "Exam not found",
                404
            ));
        }

        return Ok(ApiResponse<ExamDto>.Success(
            exam,
            "Exam retrieved successfully"
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExamCreateDto dto)
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
                "Validation failed",
                400
            ));
        }

        var exam = await _examService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = exam.Id },
            ApiResponse<ExamDto>.Success(exam, "Exam created successfully", 201)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ExamUpdateDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
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
                "Validation failed",
                400
            ));
        }

        var exam = await _examService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ExamDto>.Success(
            exam,
            "Exam updated successfully"
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _examService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Success(
            null,
            "Exam deleted successfully"
        ));
    }

    [HttpGet("{id:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid id)
    {
        var questions = await _examService.GetExamQuestionsAsync(id);
        return Ok(ApiResponse<List<ExamQuestionDto>>.Success(
            questions,
            "Exam questions retrieved successfully"
        ));
    }

    [HttpPost("{id:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid id, [FromBody] ExamQuestionCreateDto dto)
    {
        var validationResult = await _addQuestionValidator.ValidateAsync(dto);
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
                "Validation failed",
                400
            ));
        }

        var examQuestion = await _examService.AddQuestionAsync(id, dto);
        return Ok(ApiResponse<ExamQuestionDto>.Success(
            examQuestion,
            "Question added to exam successfully"
        ));
    }

    [HttpDelete("{id:guid}/questions/{examQuestionId:int}")]
    public async Task<IActionResult> RemoveQuestion(Guid id, int examQuestionId)
    {
        await _examService.RemoveQuestionAsync(id, examQuestionId);
        return Ok(ApiResponse<object>.Success(
            null,
            "Question removed from exam successfully"
        ));
    }

    [HttpPut("{id:guid}/questions/reorder")]
    public async Task<IActionResult> ReorderQuestions(Guid id, [FromBody] ReorderExamQuestionsDto dto)
    {
        var validationResult = await _reorderValidator.ValidateAsync(dto);
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
                "Validation failed",
                400
            ));
        }

        await _examService.ReorderQuestionsAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Exam questions reordered successfully"
        ));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishExamDto dto)
    {
        var validationResult = await _publishValidator.ValidateAsync(dto);
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
                "Validation failed",
                400
            ));
        }

        await _examService.PublishAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Exam published successfully"
        ));
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] ExamAssignmentCreateDto dto)
    {
        var validationResult = await _assignmentValidator.ValidateAsync(dto);
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
                "Validation failed",
                400
            ));
        }

        await _examService.AssignAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Exam assigned successfully"
        ));
    }

    [HttpGet("student/{userId:guid}/assigned")]
    public async Task<IActionResult> GetStudentAssignedExams(Guid userId)
    {
        var exams = await _examService.GetStudentAssignedExamsAsync(userId);
        return Ok(ApiResponse<List<StudentAssignedExamDto>>.Success(
            exams,
            "Assigned exams retrieved successfully"
        ));
    }
}
