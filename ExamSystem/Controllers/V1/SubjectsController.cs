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
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly IValidator<SubjectCreateDto> _createValidator;
    private readonly IValidator<SubjectUpdateDto> _updateValidator;
    private readonly IValidator<SubjectToggleActiveDto> _toggleActiveValidator;

    public SubjectsController(
        ISubjectService subjectService,
        IValidator<SubjectCreateDto> createValidator,
        IValidator<SubjectUpdateDto> updateValidator,
        IValidator<SubjectToggleActiveDto> toggleActiveValidator)
    {
        _subjectService = subjectService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _toggleActiveValidator = toggleActiveValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SubjectFilterDto filter)
    {
        var (items, total, page, pageSize) = await _subjectService.GetPagedAsync(filter);
        var pages = (int)Math.Ceiling(total / (double)pageSize);

        var paginatedResult = new PaginatedResult<SubjectDto>
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

        return Ok(ApiResponse<PaginatedResult<SubjectDto>>.Success(
            paginatedResult,
            "Lấy danh sách môn học thành công"
        ));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var subject = await _subjectService.GetByIdAsync(id);
        if (subject == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Môn học với id '{id}'" },
                "Không tìm thấy môn học",
                404
            ));
        }

        return Ok(ApiResponse<SubjectDto>.Success(
            subject,
            "Lấy thông tin môn học thành công"
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SubjectCreateDto dto)
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

        var subject = await _subjectService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = subject.Id },
            ApiResponse<SubjectDto>.Success(subject, "Tạo môn học thành công", 201)
        );
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SubjectUpdateDto dto)
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

        var subject = await _subjectService.UpdateAsync(id, dto);
        return Ok(ApiResponse<SubjectDto>.Success(
            subject,
            "Cập nhật môn học thành công"
        ));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _subjectService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Success(
            null,
            "Xóa môn học thành công"
        ));
    }

    [HttpPatch("{id:int}/active")]
    public async Task<IActionResult> ToggleActive(
        int id,
        [FromBody] SubjectToggleActiveDto dto)
    {
        // Validate body để đảm bảo isActive được cung cấp đầy đủ
        var validationResult = await _toggleActiveValidator.ValidateAsync(dto);
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

        // Cập nhật trạng thái môn học
        var subject = await _subjectService.ToggleActiveAsync(id, dto.IsActive!.Value);
        return Ok(ApiResponse<SubjectDto>.Success(
            subject,
            "Cập nhật trạng thái môn học thành công"
        ));
    }
}