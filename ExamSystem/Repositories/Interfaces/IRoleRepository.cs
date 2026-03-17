using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByIdWithPermissionsAsync(Guid id);
    Task<int> GetTotalCountAsync();
    Task<(List<Role> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<Role?> GetByNameAsync(string name);
    Task<Role> CreateAsync(Role role);
    Task<Role> UpdateAsync(Role role);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    Task<bool> AssignPermissionsAsync(Guid roleId, List<Guid> permissionIds);
    Task<List<Permission>> GetRolePermissionsAsync(Guid roleId);
}
