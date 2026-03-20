using Asp.Versioning;
using ExamSystem.Common;
using ExamSystem.DTOs;
using ExamSystem.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            "Lấy danh sách đề thi thành công"
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var exam = await _examService.GetByIdAsync(id);
        if (exam == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Đề thi với id '{id}'" },
                "Không tìm thấy đề thi",
                404
            ));
        }

        return Ok(ApiResponse<ExamDto>.Success(
            exam,
            "Lấy thông tin đề thi thành công"
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
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var exam = await _examService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = exam.Id },
            ApiResponse<ExamDto>.Success(exam, "Tạo đề thi thành công", 201)
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
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var exam = await _examService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ExamDto>.Success(
            exam,
            "Cập nhật đề thi thành công"
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _examService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Success(
            null,
            "Xóa đề thi thành công"
        ));
    }

    [HttpGet("{id:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid id)
    {
        var questions = await _examService.GetExamQuestionsAsync(id);
        return Ok(ApiResponse<List<ExamQuestionDto>>.Success(
            questions,
            "Lấy danh sách câu hỏi của đề thi thành công"
        ));
    }

    [HttpGet("{id:guid}/questions/details")]
    public async Task<IActionResult> GetQuestionDetails(Guid id)
    {
        var questions = await _examService.GetExamQuestionDetailsAsync(id);
        return Ok(ApiResponse<List<ExamQuestionDetailDto>>.Success(
            questions,
            "Lấy chi tiết câu hỏi đề thi thành công"
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
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var examQuestion = await _examService.AddQuestionAsync(id, dto);
        return Ok(ApiResponse<ExamQuestionDto>.Success(
            examQuestion,
            "Thêm câu hỏi vào đề thi thành công"
        ));
    }

    [HttpPut("{id:guid}/questions/sync")]
    public async Task<IActionResult> SyncQuestions(Guid id, [FromBody] SyncExamQuestionsDto dto)
    {
        if (dto.Items == null)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError
                {
                    Details = new List<ValidationDetail>
                    {
                        new() { Field = "Items", Message = "Danh sách câu hỏi không hợp lệ" }
                    }
                },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        await _examService.SyncExamQuestionsAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Đồng bộ danh sách câu hỏi đề thi thành công"
        ));
    }

    [HttpDelete("{id:guid}/questions/{examQuestionId:int}")]
    public async Task<IActionResult> RemoveQuestion(Guid id, int examQuestionId)
    {
        await _examService.RemoveQuestionAsync(id, examQuestionId);
        return Ok(ApiResponse<object>.Success(
            null,
            "Xóa câu hỏi khỏi đề thi thành công"
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
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        await _examService.ReorderQuestionsAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Sắp xếp lại câu hỏi đề thi thành công"
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
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        await _examService.PublishAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Công bố đề thi thành công"
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
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        await _examService.AssignAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Phân công đề thi thành công"
        ));
    }

    [HttpGet("assigned")]
    public async Task<IActionResult> GetMyAssignedExams()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Failure(
                new UnauthorizedError { Reason = "Không xác định được người dùng từ token" },
                "Không xác định được người dùng từ token",
                401
            ));
        }

        var exams = await _examService.GetStudentAssignedExamsAsync(userId);
        return Ok(ApiResponse<List<StudentAssignedExamDto>>.Success(
            exams,
            "Lấy danh sách đề thi được giao thành công"
        ));
    }
}
