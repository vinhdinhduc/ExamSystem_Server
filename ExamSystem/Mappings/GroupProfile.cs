using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;

namespace ExamSystem.Mappings;

public class GroupProfile : Profile
{
    public GroupProfile()
    {
        CreateMap<Group, GroupDto>()
            .ForCtorParam(nameof(GroupDto.Members), opt => opt.MapFrom(src => src.GroupMembers));
        CreateMap<GroupCreateDto, Group>();
        CreateMap<GroupMember, GroupMemberDto>()
            .ForCtorParam(nameof(GroupMemberDto.FullName), opt => opt.MapFrom(src => src.User.FullName))
            .ForCtorParam(nameof(GroupMemberDto.Email), opt => opt.MapFrom(src => src.User.Email));
    }
}
