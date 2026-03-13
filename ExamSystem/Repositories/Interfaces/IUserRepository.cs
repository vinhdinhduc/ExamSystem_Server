using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByIdWithRolesAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<int> GetTotalCountAsync();
    Task<(List<User> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task AssignRolesAsync(Guid userId, List<Guid> roleIds);
}
