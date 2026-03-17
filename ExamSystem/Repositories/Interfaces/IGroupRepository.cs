using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(int id);
    Task<Group?> GetByCodeAsync(string code);
    Task<(List<Group> Items, int Total)> GetPagedAsync(string? keyword, int page, int pageSize);
    Task<Group> CreateAsync(Group group);
    Task<Group> UpdateAsync(Group group);
    Task<bool> DeleteAsync(int id);

    Task<GroupMember?> GetMemberAsync(int groupId, Guid userId);
    Task AddMemberAsync(GroupMember member);
    Task RemoveMemberAsync(GroupMember member);
    Task<bool> UserExistsAsync(Guid userId);
    Task<List<Guid>> GetGroupMemberUserIdsAsync(int groupId);
    Task SaveChangesAsync();
}