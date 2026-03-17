using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IPermissionService
{
    Task<PermissionDto?> GetByIdAsync(Guid id);
    Task<(List<PermissionDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(int? page, int? pageSize);
    Task<PermissionDto> CreateAsync(PermissionCreateDto dto);
    Task<PermissionDto> UpdateAsync(Guid id, PermissionUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
