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
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IValidator<PermissionCreateDto> _createValidator;
    private readonly IValidator<PermissionUpdateDto> _updateValidator;

    public PermissionsController(
        IPermissionService permissionService,
        IValidator<PermissionCreateDto> createValidator,
        IValidator<PermissionUpdateDto> updateValidator)
    {
        _permissionService = permissionService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var (items, total, actualPage, actualPageSize) = await _permissionService.GetPagedAsync(page, pageSize);
        var pages = (int)Math.Ceiling(total / (double)actualPageSize);

        var paginatedResult = new PaginatedResult<PermissionDto>
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

        return Ok(ApiResponse<PaginatedResult<PermissionDto>>.Success(
            paginatedResult,
            "Permissions retrieved successfully"
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var permission = await _permissionService.GetByIdAsync(id);
        if (permission == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                new NotFoundError { Resource = $"Permission with id '{id}'" },
                "Permission not found",
                404
            ));
        }

        return Ok(ApiResponse<PermissionDto>.Success(
            permission,
            "Permission retrieved successfully"
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PermissionCreateDto dto)
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

        var permission = await _permissionService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = permission.Id },
            ApiResponse<PermissionDto>.Success(
                permission,
                "Permission created successfully",
                201
            )
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PermissionUpdateDto dto)
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

        var permission = await _permissionService.UpdateAsync(id, dto);
        return Ok(ApiResponse<PermissionDto>.Success(
            permission,
            "Permission updated successfully"
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _permissionService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Success(
            null,
            "Permission deleted successfully"
        ));
    }
}
