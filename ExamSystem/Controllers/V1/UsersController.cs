using System;
using System.Threading.Tasks;
using Asp.Versioning;
using ExamSystem.Common;
using ExamSystem.DTOs;
using ExamSystem.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<UserCreateDto> _createValidator;
    private readonly IValidator<UserUpdateDto> _updateValidator;
    private readonly IValidator<UserChangePasswordDto> _changePasswordValidator;
    private readonly IValidator<AssignRolesToUserDto> _assignRolesValidator;

    public UsersController(
        IUserService userService,
        IValidator<UserCreateDto> createValidator,
        IValidator<UserUpdateDto> updateValidator,
        IValidator<UserChangePasswordDto> changePasswordValidator,
        IValidator<AssignRolesToUserDto> assignRolesValidator)
    {
        _userService = userService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _changePasswordValidator = changePasswordValidator;
        _assignRolesValidator = assignRolesValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var (items, total, actualPage, actualPageSize) = await _userService.GetPagedAsync(page, pageSize);
        var pages = (int)Math.Ceiling(total / (double)actualPageSize);

        var paginatedResult = new PaginatedResult<UserDto>
        {
            Meta = new PaginationMeta
            {
                Page = actualPage,
                PageSize = actualPageSize,
                Pages = pages,
                Total = total
            },
            Result = items
        };

        return Ok(ApiResponse<PaginatedResult<UserDto>>.Success(
            paginatedResult,
            "Users retrieved successfully"
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"User with id '{id}'" },
                "User not found",
                404
            ));
        }

        return Ok(ApiResponse<UserDto>.Success(
            user,
            "User retrieved successfully"
        ));
    }

    [HttpGet("{id:guid}/roles")]
    public async Task<IActionResult> GetWithRoles(Guid id)
    {
        var user = await _userService.GetByIdWithRolesAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"User with id '{id}'" },
                "User not found",
                404
            ));
        }

        return Ok(ApiResponse<UserWithRolesDto>.Success(
            user,
            "User with roles retrieved successfully"
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Validation failed",
                400
            ));
        }

        var user = await _userService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            ApiResponse<UserDto>.Success(user, "User created successfully", 201)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Validation failed",
                400
            ));
        }

        var user = await _userService.UpdateAsync(id, dto);
        return Ok(ApiResponse<UserDto>.Success(
            user,
            "User updated successfully"
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Success(
            null,
            "User deleted successfully"
        ));
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesToUserDto dto)
    {
        var validationResult = await _assignRolesValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Validation failed",
                400
            ));
        }

        await _userService.AssignRolesAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Roles assigned to user successfully"
        ));
    }

    [HttpPost("{id:guid}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] UserChangePasswordDto dto)
    {
        var validationResult = await _changePasswordValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Validation failed",
                400
            ));
        }

        await _userService.ChangePasswordAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Password changed successfully"
        ));
    }
}
