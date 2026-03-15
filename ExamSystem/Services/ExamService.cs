using System.ComponentModel.DataAnnotations;
using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories;

namespace ExamSystem.Services;

public class ExamService : IExamService
{
    private readonly IExamRepository _examRepository;
    private readonly IMapper _mapper;

    public ExamService(IExamRepository examRepository, IMapper mapper)
    {
        _examRepository = examRepository;
        _mapper = mapper;
    }

    public async Task<List<ExamDto>> GetAllAsync()
    {
        var exams = await _examRepository.GetAllWithDetailsAsync();
        return _mapper.Map<List<ExamDto>>(exams);
    }

    public async Task<ExamDto> GetByIdAsync(Guid id)
    {
        var exam = await _examRepository.GetByIdWithDetailsAsync(id);
        if (exam is null)
        {
            throw new KeyNotFoundException("Exam not found.");
        }

        return _mapper.Map<ExamDto>(exam);
    }

    public async Task<ExamDto> CreateAsync(ExamCreateDto dto)
    {
        Validate(dto);
        var exam = _mapper.Map<Exam>(dto);
        exam.Id = Guid.NewGuid();
        exam.CreatedAt = DateTime.UtcNow;
        exam.UpdatedAt = DateTime.UtcNow;

        await _examRepository.AddAsync(exam);
        await _examRepository.SaveChangesAsync();

        return _mapper.Map<ExamDto>(exam);
    }

    public async Task<ExamDto> UpdateAsync(Guid id, ExamUpdateDto dto)
    {
        Validate(dto);
        var exam = await _examRepository.GetByIdAsync(id);
        if (exam is null)
        {
            throw new KeyNotFoundException("Exam not found.");
        }

        _mapper.Map(dto, exam);
        exam.UpdatedAt = DateTime.UtcNow;

        _examRepository.Update(exam);
        await _examRepository.SaveChangesAsync();

        return _mapper.Map<ExamDto>(exam);
    }

    public async Task DeleteAsync(Guid id)
    {
        var exam = await _examRepository.GetByIdAsync(id);
        if (exam is null)
        {
            throw new KeyNotFoundException("Exam not found.");
        }

        _examRepository.Remove(exam);
        await _examRepository.SaveChangesAsync();
    }

    private static void Validate<T>(T dto)
    {
        var context = new ValidationContext(dto!);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto!, context, results, true))
        {
            var message = string.Join("; ", results.Select(result => result.ErrorMessage));
            throw new ValidationException(message);
        }
    }
}
