using AutoMapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GroupService> _logger; 

    public GroupService(IGroupRepository groupRepository, IMapper mapper, ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<(List<GroupDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(GroupFilterDto filter)
    {
        var page = filter.Page.GetValueOrDefault(1);
        var pageSize = filter.PageSize.GetValueOrDefault(20);
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var (items, total) = await _groupRepository.GetPagedAsync(filter.Keyword, page, pageSize);
        var mappedItems = _mapper.Map<List<GroupDto>>(items);

        _logger.LogInformation(
            "GetPaged Groups => Keyword: {Keyword}, Page: {Page}, PageSize: {PageSize}, Total: {Total}, Returned: {@Groups}",
            filter.Keyword,
            page,
            pageSize,
            total,
            mappedItems.Select(g => new
            {
                g.Id,
                g.Code,
                g.Name,
                MemberCount = g.Members?.Count ?? 0,
                MemberUserIds = g.Members?.Select(m => m.UserId).ToList() ?? new List<Guid>()
            }));

        return (mappedItems, total, page, pageSize);
    }

    public async Task<GroupDto?> GetByIdAsync(int id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        _logger.LogInformation("Lấy thông tin nhóm/lớp với id {GroupId}: {Group}", id, group);
        return group == null ? null : _mapper.Map<GroupDto>(group);
    }

    public async Task<GroupDto> CreateAsync(GroupCreateDto dto)
    {
        var existing = await _groupRepository.GetByCodeAsync(dto.Code);
        if (existing != null)
        {
            throw new InvalidOperationException($"Code '{dto.Code}' đã tồn tại");
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
            throw new KeyNotFoundException($"Không tìm thấy lớp/nhóm với id '{id}'");
        }

        var existing = await _groupRepository.GetByCodeAsync(dto.Code);
        if (existing != null && existing.Id != id)
        {
            throw new InvalidOperationException($"Đã tồn tại mã code '{dto.Code}'");
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
            throw new KeyNotFoundException($"Không tìm thấy nhóm với id '{id}'");
        }
    }

    public async Task AddMemberAsync(int groupId, AddGroupMemberDto dto)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhóm với id '{groupId}'");
        }

        var userExists = await _groupRepository.UserExistsAsync(dto.UserId);
        if (!userExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy người dùng với id '{dto.UserId}'");
        }

        var member = await _groupRepository.GetMemberAsync(groupId, dto.UserId);
        if (member != null)
        {
            throw new InvalidOperationException("Người dùng đã tồn tại trong nhóm/lớp này");
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
            throw new KeyNotFoundException("Không tìm thấy thành viên nhóm");
        }

        await _groupRepository.RemoveMemberAsync(member);
        await _groupRepository.SaveChangesAsync();
    }
}
