using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;

namespace ExamSystem.Mappings;

public class PermissionProfile : Profile
{
    public PermissionProfile()
    {
        CreateMap<Permission, PermissionDto>();
        CreateMap<PermissionCreateDto, Permission>();
        CreateMap<PermissionUpdateDto, Permission>();
    }
}
