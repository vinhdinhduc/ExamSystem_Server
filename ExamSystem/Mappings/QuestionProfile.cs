using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;

namespace ExamSystem.Mappings;

public class QuestionProfile : Profile
{
    public QuestionProfile()
    {
        // ConvertUsing: tránh lỗi map record + Answers khi có code gọi Map<List<QuestionDto>>(questions)
        CreateMap<Question, QuestionDto>().ConvertUsing((Question q) => QuestionEntityMapper.ToDto(q));
    }
}
