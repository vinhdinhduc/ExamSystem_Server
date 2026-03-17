using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly ExamSystemDbContext _context;

    public GroupRepository(ExamSystemDbContext context)
    {
        _context = context;
    }

    public Task<Group?> GetByIdAsync(int id)
        => _context.Groups
            .Include(g => g.GroupMembers)
            .FirstOrDefaultAsync(g => g.Id == id);

    public Task<Group?> GetByCodeAsync(string code)
        => _context.Groups.FirstOrDefaultAsync(g => g.Code == code);

    public async Task<(List<Group> Items, int Total)> GetPagedAsync(string? keyword, int page, int pageSize)
    {
        var query = _context.Groups
            .Include(g => g.GroupMembers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(g => g.Name.Contains(keyword) || g.Code.Contains(keyword));
        }

        query = query.OrderBy(g => g.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Group> CreateAsync(Group group)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<Group> UpdateAsync(Group group)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
        {
            return false;
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<GroupMember?> GetMemberAsync(int groupId, Guid userId)
        => _context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

    public Task AddMemberAsync(GroupMember member)
        => _context.GroupMembers.AddAsync(member).AsTask();

    public Task RemoveMemberAsync(GroupMember member)
    {
        _context.GroupMembers.Remove(member);
        return Task.CompletedTask;
    }

    public Task<bool> UserExistsAsync(Guid userId)
        => _context.Users.AnyAsync(u => u.Id == userId);

    public Task<List<Guid>> GetGroupMemberUserIdsAsync(int groupId)
        => _context.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync();

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
