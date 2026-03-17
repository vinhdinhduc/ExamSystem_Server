using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;

namespace ExamSystem.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UserCreateDto, User>();
        CreateMap<UserUpdateDto, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
