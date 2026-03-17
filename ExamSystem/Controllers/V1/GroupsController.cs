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
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IValidator<GroupCreateDto> _createValidator;
    private readonly IValidator<GroupUpdateDto> _updateValidator;
    private readonly IValidator<AddGroupMemberDto> _addMemberValidator;

    public GroupsController(
        IGroupService groupService,
        IValidator<GroupCreateDto> createValidator,
        IValidator<GroupUpdateDto> updateValidator,
        IValidator<AddGroupMemberDto> addMemberValidator)
    {
        _groupService = groupService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addMemberValidator = addMemberValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GroupFilterDto filter)
    {
        var (items, total, page, pageSize) = await _groupService.GetPagedAsync(filter);
        var pages = (int)Math.Ceiling(total / (double)pageSize);

        var paginatedResult = new PaginatedResult<GroupDto>
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

        return Ok(ApiResponse<PaginatedResult<GroupDto>>.Success(
            paginatedResult,
            "Groups retrieved successfully"
        ));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Group with id '{id}'" },
                "Group not found",
                404
            ));
        }

        return Ok(ApiResponse<GroupDto>.Success(
            group,
            "Group retrieved successfully"
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GroupCreateDto dto)
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

        var group = await _groupService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = group.Id },
            ApiResponse<GroupDto>.Success(group, "Group created successfully", 201)
        );
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GroupUpdateDto dto)
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

        var group = await _groupService.UpdateAsync(id, dto);
        return Ok(ApiResponse<GroupDto>.Success(
            group,
            "Group updated successfully"
        ));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _groupService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Success(
            null,
            "Group deleted successfully"
        ));
    }

    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddGroupMemberDto dto)
    {
        var validationResult = await _addMemberValidator.ValidateAsync(dto);
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

        await _groupService.AddMemberAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Group member added successfully"
        ));
    }

    [HttpDelete("{id:int}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(int id, Guid userId)
    {
        await _groupService.RemoveMemberAsync(id, userId);
        return Ok(ApiResponse<object>.Success(
            null,
            "Group member removed successfully"
        ));
    }
}
