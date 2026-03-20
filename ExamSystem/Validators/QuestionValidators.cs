using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class QuestionCreateDtoValidator : AbstractValidator<QuestionCreateDto>
{
    public QuestionCreateDtoValidator()
    {
        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage("Mã môn học phải lớn hơn 0");

        RuleFor(x => x.CreatedByUserId)
            .NotNull().WithMessage("Người tạo không được để trống")
            .Must(x => x.HasValue && x.Value != Guid.Empty)
            .WithMessage("Người tạo không hợp lệ");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung câu hỏi không được để trống")
            .MaximumLength(4000).WithMessage("Nội dung câu hỏi không được vượt quá 4000 ký tự");

        RuleFor(x => x.QuestionType)
            .InclusiveBetween((byte)0, (byte)2).WithMessage("Loại câu hỏi không hợp lệ");

        RuleFor(x => x.DifficultyLevel)
            .InclusiveBetween((byte)1, (byte)3).WithMessage("Mức độ khó không hợp lệ");

        RuleFor(x => x.Options)
            .Must(options => options == null || options.Count >= 2)
            .WithMessage("Nếu có đáp án, phải có ít nhất 2 đáp án");

        RuleForEach(x => x.Options!).ChildRules(option =>
        {
            option.RuleFor(o => o.Content)
                .NotEmpty().WithMessage("Nội dung đáp án không được để trống")
                .MaximumLength(1000).WithMessage("Nội dung đáp án không được vượt quá 1000 ký tự");
        }).When(x => x.Options is { Count: > 0 });
    }
}