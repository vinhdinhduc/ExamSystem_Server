using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ExamSystem.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IFileService _fileService;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IMapper mapper,
        IPasswordHasher<User> passwordHasher,
        IFileService fileService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _fileService = fileService;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserWithRolesDto?> GetByIdWithRolesAsync(Guid id)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(id);
        if (user == null)
            return null;

        var roles = user.UserRoles
            .Select(ur => _mapper.Map<RoleDto>(ur.Role))
            .ToList();

        return new UserWithRolesDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Avatar,
            user.IsActive,
            user.CreatedAt,
            roles
        );
    }

    public async Task<(List<UserDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(int? page, int? pageSize)
    {
        var total = await _userRepository.GetTotalCountAsync();

        int actualPage = page ?? 1;
        int actualPageSize = pageSize ?? total;

        if (actualPageSize <= 0) actualPageSize = total > 0 ? total : 1;

        var (items, _) = await _userRepository.GetPagedAsync(actualPage, actualPageSize);
        return (_mapper.Map<List<UserDto>>(items), total, actualPage, actualPageSize);
    }

    public async Task<(List<UserListItemDto> Items, int Total, int Page, int PageSize)> GetPagedWithRolesAsync(int? page, int? pageSize)
    {
        var total = await _userRepository.GetTotalCountAsync();

        int actualPage = page ?? 1;
        int actualPageSize = pageSize ?? 20;

        if (actualPageSize <= 0) actualPageSize = 20;

        var (items, _) = await _userRepository.GetPagedWithRolesAsync(actualPage, actualPageSize);

        var dtos = items.Select(u => new UserListItemDto(
            u.Id,
            u.Username,
            u.Email,
            u.FullName,
            u.Avatar,
            u.IsActive,
            u.CreatedAt,
            u.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).ToList()
        )).ToList();

        return (dtos, total, actualPage, actualPageSize);
    }

    public async Task<UserDto> CreateAsync(UserCreateDto dto)
    {
        // Check if username already exists
        if (await _userRepository.ExistsByUsernameAsync(dto.Username))
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken");

        // Check if email already exists
        if (await _userRepository.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            FullName = dto.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Hash password
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        var created = await _userRepository.CreateAsync(user);
        return _mapper.Map<UserDto>(created);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{id}' not found");

        // Update only provided fields
        if (dto.Username != null)
        {
            // Check if new username is already taken by another user
            var existingUser = await _userRepository.GetByUsernameAsync(dto.Username);
            if (existingUser != null && existingUser.Id != id)
                throw new InvalidOperationException($"Username '{dto.Username}' is already taken");

            user.Username = dto.Username;
        }

        if (dto.Email != null)
        {
            // Check if new email is already registered by another user
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null && existingUser.Id != id)
                throw new InvalidOperationException($"Email '{dto.Email}' is already registered");

            user.Email = dto.Email;
        }

        if (dto.FullName != null)
            user.FullName = dto.FullName;

        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{id}' not found");

        await _userRepository.DeleteAsync(user);
    }

    public async Task AssignRolesAsync(Guid id, AssignRolesToUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{id}' not found");

        // Validate that all roles exist
        foreach (var roleId in dto.RoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
                throw new KeyNotFoundException($"Role with id '{roleId}' not found");
        }

        await _userRepository.AssignRolesAsync(id, dto.RoleIds);
    }

    public async Task<UserDto> ToggleLockAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"Người dùng với id '{id}' không tồn tại");

        user.IsActive = !user.IsActive;
        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserDto>(updated);
    }

    public async Task ChangePasswordAsync(Guid id, UserChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{id}' not found");

        // Verify current password
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect");

        // Hash and set new password
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        await _userRepository.UpdateAsync(user);
    }

    public async Task<UserDto> UploadAvatarAsync(Guid id, IFormFile file)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{id}' not found");

        // Xóa avatar cũ nếu có
        _fileService.DeleteAvatar(user.Avatar);

        // Lưu file mới
        var fileName = await _fileService.SaveAvatarAsync(file);
        user.Avatar = fileName;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserDto>(updated);
    }
}
