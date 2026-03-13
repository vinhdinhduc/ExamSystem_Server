using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IRoleService
{
    Task<RoleDto?> GetByIdAsync(Guid id);
    Task<RoleWithPermissionsDto?> GetByIdWithPermissionsAsync(Guid id);
    Task<(List<RoleDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(int? page, int? pageSize);
    Task<RoleDto> CreateAsync(RoleCreateDto dto);
    Task<RoleDto> UpdateAsync(Guid id, RoleUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> AssignPermissionsAsync(Guid roleId, AssignPermissionsToRoleDto dto);
}
