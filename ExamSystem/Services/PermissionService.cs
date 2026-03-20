using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<PermissionDto?> GetByIdAsync(Guid id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        return permission == null ? null : _mapper.Map<PermissionDto>(permission);
    }

    public async Task<(List<PermissionDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(int? page, int? pageSize)
    {
        // Nếu không truyền params, lấy tất cả (pageSize = total)
        var total = await _permissionRepository.GetTotalCountAsync();

        int actualPage = page ?? 1;
        int actualPageSize = pageSize ?? total;

        if (actualPageSize <= 0) actualPageSize = total > 0 ? total : 1;

        var (items, _) = await _permissionRepository.GetPagedAsync(actualPage, actualPageSize);
        return (_mapper.Map<List<PermissionDto>>(items), total, actualPage, actualPageSize);
    }

    public async Task<PermissionDto> CreateAsync(PermissionCreateDto dto)
    {
        if (await _permissionRepository.ExistsByCodeAsync(dto.Code))
            throw new InvalidOperationException($"Mã quyền '{dto.Code}' đã tồn tại");

        var permission = _mapper.Map<Permission>(dto);
        var created = await _permissionRepository.CreateAsync(permission);
        return _mapper.Map<PermissionDto>(created);
    }

    public async Task<PermissionDto> UpdateAsync(Guid id, PermissionUpdateDto dto)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
            throw new KeyNotFoundException($"Không tìm thấy quyền với id '{id}'");

        if (await _permissionRepository.ExistsByCodeAsync(dto.Code, id))
            throw new InvalidOperationException($"Mã quyền '{dto.Code}' đã tồn tại");

        permission.Code = dto.Code;
        permission.Description = dto.Description;

        var updated = await _permissionRepository.UpdateAsync(permission);
        return _mapper.Map<PermissionDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (!await _permissionRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Không tìm thấy quyền với id '{id}'");

        return await _permissionRepository.DeleteAsync(id);
    }
}
