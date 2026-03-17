using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;

namespace ExamSystem.Mappings;

public class ExamProfile : Profile
{
    public ExamProfile()
    {
        CreateMap<Exam, ExamDto>();
        CreateMap<ExamCreateDto, Exam>();
        CreateMap<ExamQuestion, ExamQuestionDto>();
    }
}
