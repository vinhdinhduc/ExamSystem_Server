using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;

namespace ExamSystem.Mappings;

public class QuestionProfile : Profile
{
    public QuestionProfile()
    {
        CreateMap<Question, QuestionDto>();
    }
}