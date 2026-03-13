using System;
using System.Threading.Tasks;
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
            "Roles retrieved successfully"
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Role with id '{id}'" },
                "Role not found",
                404
            ));
        }

        return Ok(ApiResponse<RoleDto>.Success(
            role,
            "Role retrieved successfully"
        ));
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> GetWithPermissions(Guid id)
    {
        var role = await _roleService.GetByIdWithPermissionsAsync(id);
        if (role == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Role with id '{id}'" },
                "Role not found",
                404
            ));
        }

        return Ok(ApiResponse<RoleWithPermissionsDto>.Success(
            role,
            "Role with permissions retrieved successfully"
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var validationError = new ValidationError
            {
                Details = validationResult.Errors.Select(e => new ValidationDetail
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList()
            };

            return BadRequest(ApiResponse<object>.Failure(
                validationError,
                "Validation failed",
                400
            ));
        }

        var role = await _roleService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = role.Id },
            ApiResponse<RoleDto>.Success(
                role,
                "Role created successfully",
                201
            )
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RoleUpdateDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var validationError = new ValidationError
            {
                Details = validationResult.Errors.Select(e => new ValidationDetail
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList()
            };

            return BadRequest(ApiResponse<object>.Failure(
                validationError,
                "Validation failed",
                400
            ));
        }

        var role = await _roleService.UpdateAsync(id, dto);
        return Ok(ApiResponse<RoleDto>.Success(
            role,
            "Role updated successfully"
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _roleService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Success(
            null,
            "Role deleted successfully"
        ));
    }

    [HttpPost("{id:guid}/permissions")]
    public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignPermissionsToRoleDto dto)
    {
        var validationResult = await _assignPermissionsValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var validationError = new ValidationError
            {
                Details = validationResult.Errors.Select(e => new ValidationDetail
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList()
            };

            return BadRequest(ApiResponse<object>.Failure(
                validationError,
                "Validation failed",
                400
            ));
        }

        await _roleService.AssignPermissionsAsync(id, dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Permissions assigned to role successfully"
        ));
    }
}
