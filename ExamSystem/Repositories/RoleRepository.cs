using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ExamSystemDbContext _context;

    public RoleRepository(ExamSystemDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task<Role?> GetByIdWithPermissionsAsync(Guid id)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Roles.CountAsync();
    }

    public async Task<(List<Role> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Roles.OrderBy(r => r.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<Role> CreateAsync(Role role)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<Role> UpdateAsync(Role role)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
            return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Roles.AnyAsync(r => r.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var query = _context.Roles.Where(r => r.Name == name);
        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<bool> AssignPermissionsAsync(Guid roleId, List<Guid> permissionIds)
    {
        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        _context.RolePermissions.RemoveRange(existingPermissions);

        var newRolePermissions = permissionIds
            .Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            })
            .ToList();

        await _context.RolePermissions.AddRangeAsync(newRolePermissions);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<Permission>> GetRolePermissionsAsync(Guid roleId)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }
}
