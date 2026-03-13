using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(Guid id);
    Task<int> GetTotalCountAsync();
    Task<(List<Permission> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<Permission?> GetByCodeAsync(string code);
    Task<Permission> CreateAsync(Permission permission);
    Task<Permission> UpdateAsync(Permission permission);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null);
    Task<List<Permission>> GetByIdsAsync(List<Guid> ids);
}
