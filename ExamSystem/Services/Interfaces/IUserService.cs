using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserWithRolesDto?> GetByIdWithRolesAsync(Guid id);
    Task<(List<UserDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(int? page, int? pageSize);
    Task<(List<UserListItemDto> Items, int Total, int Page, int PageSize)> GetPagedWithRolesAsync(int? page, int? pageSize);
    Task<UserDto> CreateAsync(UserCreateDto dto);
    Task<UserDto> UpdateAsync(Guid id, UserUpdateDto dto);
    Task DeleteAsync(Guid id);
    Task AssignRolesAsync(Guid id, AssignRolesToUserDto dto);
    Task ChangePasswordAsync(Guid id, UserChangePasswordDto dto);
    Task<UserDto> ToggleLockAsync(Guid id);
}
