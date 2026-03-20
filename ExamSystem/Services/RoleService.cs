using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public RoleService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMapper mapper)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        return role == null ? null : _mapper.Map<RoleDto>(role);
    }

    public async Task<RoleWithPermissionsDto?> GetByIdWithPermissionsAsync(Guid id)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(id);
        if (role == null)
            return null;

        var permissions = role.RolePermissions
            .Select(rp => _mapper.Map<PermissionDto>(rp.Permission))
            .ToList();

        return new RoleWithPermissionsDto(
            role.Id,
            role.Name,
            role.Description,
            role.CreatedAt,
            permissions
        );
    }

    public async Task<(List<RoleDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(int? page, int? pageSize)
    {
        // Nếu không truyền params, lấy tất cả (pageSize = total)
        var total = await _roleRepository.GetTotalCountAsync();

        int actualPage = page ?? 1;
        int actualPageSize = pageSize ?? total;

        if (actualPageSize <= 0) actualPageSize = total > 0 ? total : 1;

        var (items, _) = await _roleRepository.GetPagedAsync(actualPage, actualPageSize);
        return (_mapper.Map<List<RoleDto>>(items), total, actualPage, actualPageSize);
    }

    public async Task<RoleDto> CreateAsync(RoleCreateDto dto)
    {
        if (await _roleRepository.ExistsByNameAsync(dto.Name))
            throw new InvalidOperationException($"Tên vai trò '{dto.Name}' đã tồn tại");

        var role = _mapper.Map<Role>(dto);
        role.CreatedAt = DateTime.UtcNow;

        var created = await _roleRepository.CreateAsync(role);
        return _mapper.Map<RoleDto>(created);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, RoleUpdateDto dto)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            throw new KeyNotFoundException($"Không tìm thấy vai trò với id '{id}'");

        if (await _roleRepository.ExistsByNameAsync(dto.Name, id))
            throw new InvalidOperationException($"Tên vai trò '{dto.Name}' đã tồn tại");

        role.Name = dto.Name;
        role.Description = dto.Description;

        var updated = await _roleRepository.UpdateAsync(role);
        return _mapper.Map<RoleDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (!await _roleRepository.ExistsAsync(id))
            throw new KeyNotFoundException($"Không tìm thấy vai trò với id '{id}'");

        return await _roleRepository.DeleteAsync(id);
    }

    public async Task<bool> AssignPermissionsAsync(Guid roleId, AssignPermissionsToRoleDto dto)
    {
        if (!await _roleRepository.ExistsAsync(roleId))
            throw new KeyNotFoundException($"Không tìm thấy vai trò với id '{roleId}'");

        var existingPermissions = await _permissionRepository.GetByIdsAsync(dto.PermissionIds);
        if (existingPermissions.Count != dto.PermissionIds.Count)
        {
            var missingIds = dto.PermissionIds.Except(existingPermissions.Select(p => p.Id)).ToList();
            throw new InvalidOperationException($"Không tìm thấy một số quyền: {string.Join(", ", missingIds)}");
        }

        return await _roleRepository.AssignPermissionsAsync(roleId, dto.PermissionIds);
    }
}
