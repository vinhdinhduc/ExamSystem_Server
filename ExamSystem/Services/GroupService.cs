using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;

    public GroupService(IGroupRepository groupRepository, IMapper mapper)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
    }

    public async Task<(List<GroupDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(GroupFilterDto filter)
    {
        var page = filter.Page.GetValueOrDefault(1);
        var pageSize = filter.PageSize.GetValueOrDefault(20);
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var (items, total) = await _groupRepository.GetPagedAsync(filter.Keyword, page, pageSize);
        return (_mapper.Map<List<GroupDto>>(items), total, page, pageSize);
    }

    public async Task<GroupDto?> GetByIdAsync(int id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        return group == null ? null : _mapper.Map<GroupDto>(group);
    }

    public async Task<GroupDto> CreateAsync(GroupCreateDto dto)
    {
        var existing = await _groupRepository.GetByCodeAsync(dto.Code);
        if (existing != null)
        {
            throw new InvalidOperationException($"Group code '{dto.Code}' already exists");
        }

        var group = _mapper.Map<Group>(dto);
        group.CreatedAt = DateTime.UtcNow;

        var created = await _groupRepository.CreateAsync(group);
        return _mapper.Map<GroupDto>(created);
    }

    public async Task<GroupDto> UpdateAsync(int id, GroupUpdateDto dto)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with id '{id}' not found");
        }

        var existing = await _groupRepository.GetByCodeAsync(dto.Code);
        if (existing != null && existing.Id != id)
        {
            throw new InvalidOperationException($"Group code '{dto.Code}' already exists");
        }

        group.Name = dto.Name;
        group.Code = dto.Code;
        group.Description = dto.Description;

        var updated = await _groupRepository.UpdateAsync(group);
        return _mapper.Map<GroupDto>(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var deleted = await _groupRepository.DeleteAsync(id);
        if (!deleted)
        {
            throw new KeyNotFoundException($"Group with id '{id}' not found");
        }
    }

    public async Task AddMemberAsync(int groupId, AddGroupMemberDto dto)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with id '{groupId}' not found");
        }

        var userExists = await _groupRepository.UserExistsAsync(dto.UserId);
        if (!userExists)
        {
            throw new KeyNotFoundException($"User with id '{dto.UserId}' not found");
        }

        var member = await _groupRepository.GetMemberAsync(groupId, dto.UserId);
        if (member != null)
        {
            throw new InvalidOperationException("User already in group");
        }

        await _groupRepository.AddMemberAsync(new GroupMember
        {
            GroupId = groupId,
            UserId = dto.UserId,
            JoinedAt = DateTime.UtcNow
        });

        await _groupRepository.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(int groupId, Guid userId)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
        {
            throw new KeyNotFoundException("Group member not found");
        }

        await _groupRepository.RemoveMemberAsync(member);
        await _groupRepository.SaveChangesAsync();
    }
}
