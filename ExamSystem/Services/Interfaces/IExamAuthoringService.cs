using ExamSystem.DTOs;

namespace ExamSystem.Services.Interfaces;

public interface IExamAuthoringService
{
    Task<ExamAuthoringResultDto> GenerateWithGeminiAsync(GenerateExamWithGeminiRequestDto dto);
    Task<ExamAuthoringResultDto> ImportFromFileAsync(ImportExamFromFileRequestDto dto);
    Task<ExamAuthoringResultDto> SaveDraftAsync(SaveExamDraftRequestDto dto);
}
