using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly ExamSystemDbContext _context;

    public PermissionRepository(ExamSystemDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(Guid id)
    {
        return await _context.Permissions.FindAsync(id);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Permissions.CountAsync();
    }

    public async Task<(List<Permission> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Permissions.OrderBy(p => p.Code);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Permission?> GetByCodeAsync(string code)
    {
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<Permission> CreateAsync(Permission permission)
    {
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();
        return permission;
    }

    public async Task<Permission> UpdateAsync(Permission permission)
    {
        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync();
        return permission;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
            return false;

        _context.Permissions.Remove(permission);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Permissions.AnyAsync(p => p.Id == id);
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
    {
        var query = _context.Permissions.Where(p => p.Code == code);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<List<Permission>> GetByIdsAsync(List<Guid> ids)
    {
        return await _context.Permissions
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }
}
