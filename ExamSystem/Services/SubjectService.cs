using AutoMapper;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public SubjectService(ISubjectRepository subjectRepository, IMapper mapper)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<(List<SubjectDto> Items, int Total, int Page, int PageSize)> GetPagedAsync(SubjectFilterDto filter)
    {
        var page = filter.Page.GetValueOrDefault(1);
        var pageSize = filter.PageSize.GetValueOrDefault(20);
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var (items, total) = await _subjectRepository.GetPagedAsync(filter.Keyword, filter.IsActive, page, pageSize);
        return (_mapper.Map<List<SubjectDto>>(items), total, page, pageSize);
    }

    public async Task<SubjectDto?> GetByIdAsync(int id)
    {
        var subject = await _subjectRepository.GetByIdAsync(id);
        return subject == null ? null : _mapper.Map<SubjectDto>(subject);
    }

    public async Task<SubjectDto> CreateAsync(SubjectCreateDto dto)
    {
        var existing = await _subjectRepository.GetByCodeAsync(dto.Code);
        if (existing != null)
        {
            throw new InvalidOperationException($"Subject code '{dto.Code}' already exists");
        }

        var subject = _mapper.Map<Subject>(dto);
        subject.CreatedAt = DateTime.UtcNow;

        var created = await _subjectRepository.CreateAsync(subject);
        return _mapper.Map<SubjectDto>(created);
    }

    public async Task<SubjectDto> UpdateAsync(int id, SubjectUpdateDto dto)
    {
        var subject = await _subjectRepository.GetByIdAsync(id);
        if (subject == null)
        {
            throw new KeyNotFoundException($"Subject with id '{id}' not found");
        }

        var existing = await _subjectRepository.GetByCodeAsync(dto.Code);
        if (existing != null && existing.Id != id)
        {
            throw new InvalidOperationException($"Subject code '{dto.Code}' already exists");
        }

        subject.Name = dto.Name;
        subject.Code = dto.Code;
        subject.Description = dto.Description;
        subject.IsActive = dto.IsActive;

        var updated = await _subjectRepository.UpdateAsync(subject);
        return _mapper.Map<SubjectDto>(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var deleted = await _subjectRepository.DeleteAsync(id);
        if (!deleted)
        {
            throw new KeyNotFoundException($"Subject with id '{id}' not found");
        }
    }
}
