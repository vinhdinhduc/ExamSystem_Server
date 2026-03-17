using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;

namespace ExamSystem.Mappings;

public class GroupProfile : Profile
{
    public GroupProfile()
    {
        CreateMap<Group, GroupDto>();
        CreateMap<GroupCreateDto, Group>();
        CreateMap<GroupMember, GroupMemberDto>();
    }
}
