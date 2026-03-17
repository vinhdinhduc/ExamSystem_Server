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
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly IValidator<RoleCreateDto> _createValidator;
    private readonly IValidator<RoleUpdateDto> _updateValidator;
    private readonly IValidator<AssignPermissionsToRoleDto> _assignPermissionsValidator;

    public RolesController(
        IRoleService roleService,
        IValidator<RoleCreateDto> createValidator,
        IValidator<RoleUpdateDto> updateValidator,
        IValidator<AssignPermissionsToRoleDto> assignPermissionsValidator)
    {
        _roleService = roleService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _assignPermissionsValidator = assignPermissionsValidator;
    }

    [HttpGet]
    [RequirePermission(Permissions.RoleView)]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var (items, total, actualPage, actualPageSize) = await _roleService.GetPagedAsync(page, pageSize);
        var pages = (int)Math.Ceiling(total / (double)actualPageSize);

        var paginatedResult = new PaginatedResult<RoleDto>
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

        return Ok(ApiResponse<PaginatedResult<RoleDto>>.Success(
            paginatedResult,
            "Lấy danh sách vai trò thành công"
        ));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.RoleView)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Vai trò với id '{id}'" },
                "Không tìm thấy vai trò",
                404
            ));
        }

        return Ok(ApiResponse<RoleDto>.Success(role, "Lấy thông tin vai trò thành công"));
    }

    [HttpGet("{id:guid}/permissions")]
    [RequirePermission(Permissions.RoleView)]
    public async Task<IActionResult> GetWithPermissions(Guid id)
    {
        var role = await _roleService.GetByIdWithPermissionsAsync(id);
        if (role == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Vai trò với id '{id}'" },
                "Không tìm thấy vai trò",
                404
            ));
        }

        return Ok(ApiResponse<RoleWithPermissionsDto>.Success(role, "Lấy vai trò kèm quyền thành công"));
    }

    [HttpPost]
    [RequirePermission(Permissions.RoleCreate)]
    public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
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

        var role = await _roleService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = role.Id },
            ApiResponse<RoleDto>.Success(role, "Tạo vai trò thành công", 201)
        );
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.RoleUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] RoleUpdateDto dto)
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

        var role = await _roleService.UpdateAsync(id, dto);
        return Ok(ApiResponse<RoleDto>.Success(role, "Cập nhật vai trò thành công"));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.RoleDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _roleService.DeleteAsync(id);
        return Ok(ApiResponse<object?>.Success(null, "Xóa vai trò thành công"));
    }

    [HttpPost("{id:guid}/permissions")]
    [RequirePermission(Permissions.RoleAssignPermission)]
    public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignPermissionsToRoleDto dto)
    {
        var validationResult = await _assignPermissionsValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        await _roleService.AssignPermissionsAsync(id, dto);
        return Ok(ApiResponse<object?>.Success(null, "Gán quyền cho vai trò thành công"));
    }
}
