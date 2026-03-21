using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class GenerateExamWithGeminiRequestDtoValidator : AbstractValidator<GenerateExamWithGeminiRequestDto>
{
    public GenerateExamWithGeminiRequestDtoValidator()
    {
        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage("Mã môn học phải lớn hơn 0");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("Người tạo không được để trống");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề đề thi không được để trống")
            .MaximumLength(255).WithMessage("Tiêu đề đề thi không được vượt quá 255 ký tự");

        RuleFor(x => x.QuestionCount)
            .InclusiveBetween(1, 200).WithMessage("Số lượng câu hỏi phải từ 1 đến 200");

        RuleFor(x => x.Duration)
            .GreaterThan(0).WithMessage("Thời gian làm bài phải lớn hơn 0");

        RuleFor(x => x.PassScore)
            .GreaterThanOrEqualTo(0).WithMessage("Điểm đạt phải lớn hơn hoặc bằng 0");

        RuleFor(x => x.MaxAttempts)
            .GreaterThan(0).WithMessage("Số lần làm tối đa phải lớn hơn 0");

        RuleFor(x => x.Status)
            .InclusiveBetween((byte)0, (byte)2).WithMessage("Trạng thái không hợp lệ");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
            .WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu");
    }
}

public class SaveExamDraftRequestDtoValidator : AbstractValidator<SaveExamDraftRequestDto>
{
    public SaveExamDraftRequestDtoValidator()
    {
        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage("Mã môn học phải lớn hơn 0");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("Người tạo không được để trống");

        RuleFor(x => x.Draft)
            .NotNull().WithMessage("Nháp đề thi không được để trống");

        RuleFor(x => x.Draft.Title)
            .NotEmpty().WithMessage("Tiêu đề đề thi không được để trống")
            .MaximumLength(255).WithMessage("Tiêu đề đề thi không được vượt quá 255 ký tự");

        RuleFor(x => x.Draft.Duration)
            .GreaterThan(0).WithMessage("Thời gian làm bài phải lớn hơn 0");

        RuleFor(x => x.Draft.MaxAttempts)
            .GreaterThan(0).WithMessage("Số lần làm tối đa phải lớn hơn 0");

        RuleFor(x => x.Draft.Status)
            .InclusiveBetween((byte)0, (byte)2).WithMessage("Trạng thái không hợp lệ");

        RuleFor(x => x.Draft.Questions)
            .NotNull().WithMessage("Danh sách câu hỏi không được để trống")
            .Must(q => q is { Count: > 0 }).WithMessage("Nháp đề thi phải có ít nhất 1 câu hỏi");

        RuleFor(x => x)
            .Must(x => !x.Draft.StartDate.HasValue || !x.Draft.EndDate.HasValue || x.Draft.EndDate.Value >= x.Draft.StartDate.Value)
            .WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu");
    }
}

public class ImportExamFromFileRequestDtoValidator : AbstractValidator<ImportExamFromFileRequestDto>
{
    public ImportExamFromFileRequestDtoValidator()
    {
        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage("Mã môn học phải lớn hơn 0");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("Người tạo không được để trống");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề đề thi không được để trống")
            .MaximumLength(255).WithMessage("Tiêu đề đề thi không được vượt quá 255 ký tự");

        RuleFor(x => x.Duration)
            .GreaterThan(0).WithMessage("Thời gian làm bài phải lớn hơn 0");

        RuleFor(x => x.PassScore)
            .GreaterThanOrEqualTo(0).WithMessage("Điểm đạt phải lớn hơn hoặc bằng 0");

        RuleFor(x => x.MaxAttempts)
            .GreaterThan(0).WithMessage("Số lần làm tối đa phải lớn hơn 0");

        RuleFor(x => x.Status)
            .InclusiveBetween((byte)0, (byte)2).WithMessage("Trạng thái không hợp lệ");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File không được để trống")
            .Must(f => f is { Length: > 0 }).WithMessage("File không hợp lệ hoặc rỗng");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
            .WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu");
    }
}
