using System;
using System.Threading.Tasks;
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
    [RequirePermission(Permissions.UserView)]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var (items, total, actualPage, actualPageSize) = await _userService.GetPagedWithRolesAsync(page, pageSize);
        var pages = (int)Math.Ceiling(total / (double)actualPageSize);

        var paginatedResult = new PaginatedResult<UserListItemDto>
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

        return Ok(ApiResponse<PaginatedResult<UserListItemDto>>.Success(
            paginatedResult,
            "Lấy danh sách người dùng thành công"
        ));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.UserView)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Người dùng với id '{id}'" },
                "Không tìm thấy người dùng",
                404
            ));
        }

        return Ok(ApiResponse<UserDto>.Success(user, "Lấy thông tin người dùng thành công"));
    }

    [HttpGet("{id:guid}/roles")]
    [RequirePermission(Permissions.UserView)]
    public async Task<IActionResult> GetWithRoles(Guid id)
    {
        var user = await _userService.GetByIdWithRolesAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Người dùng với id '{id}'" },
                "Không tìm thấy người dùng",
                404
            ));
        }

        return Ok(ApiResponse<UserWithRolesDto>.Success(user, "Lấy người dùng kèm vai trò thành công"));
    }

    [HttpPost]
    [RequirePermission(Permissions.UserCreate)]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var user = await _userService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            ApiResponse<UserDto>.Success(user, "Tạo người dùng thành công", 201)
        );
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.UserUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var user = await _userService.UpdateAsync(id, dto);
        return Ok(ApiResponse<UserDto>.Success(user, "Cập nhật người dùng thành công"));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.UserDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.DeleteAsync(id);
        return Ok(ApiResponse<object?>.Success(null, "Xóa người dùng thành công"));
    }

    [HttpPost("{id:guid}/roles")]
    [RequirePermission(Permissions.UserUpdate)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesToUserDto dto)
    {
        var validationResult = await _assignRolesValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        await _userService.AssignRolesAsync(id, dto);
        return Ok(ApiResponse<object?>.Success(null, "Gán vai trò cho người dùng thành công"));
    }

    [HttpPatch("{id:guid}/toggle-lock")]
    [RequirePermission(Permissions.UserUpdate)]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        var user = await _userService.ToggleLockAsync(id);
        var action = user.IsActive ? "mở khóa" : "khóa";
        return Ok(ApiResponse<UserDto>.Success(user, $"Đã {action} tài khoản thành công"));
    }

    [HttpPost("{id:guid}/change-password")]
    [RequirePermission(Permissions.UserUpdate)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] UserChangePasswordDto dto)
    {
        var validationResult = await _changePasswordValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        await _userService.ChangePasswordAsync(id, dto);
        return Ok(ApiResponse<object?>.Success(null, "Đổi mật khẩu thành công"));
    }
}
