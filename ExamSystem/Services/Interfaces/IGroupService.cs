using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IGroupService
{
    Task<(List<GroupDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(GroupFilterDto filter);
    Task<GroupDto?> GetByIdAsync(int id);
    Task<GroupDto> CreateAsync(GroupCreateDto dto);
    Task<GroupDto> UpdateAsync(int id, GroupUpdateDto dto);
    Task DeleteAsync(int id);

    Task AddMemberAsync(int groupId, AddGroupMemberDto dto);
    Task RemoveMemberAsync(int groupId, Guid userId);
}