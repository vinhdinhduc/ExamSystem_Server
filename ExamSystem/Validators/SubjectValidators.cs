using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class SubjectCreateDtoValidator : AbstractValidator<SubjectCreateDto>
{
    public SubjectCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên môn học không được để trống")
            .MaximumLength(200).WithMessage("Tên môn học không được vượt quá 200 ký tự");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã môn học không được để trống")
            .MaximumLength(50).WithMessage("Mã môn học không được vượt quá 50 ký tự")
            .Matches(@"^[A-Z0-9_]+$").WithMessage("Mã môn học chỉ được chứa chữ in hoa, số và dấu gạch dưới");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public class SubjectUpdateDtoValidator : AbstractValidator<SubjectUpdateDto>
{
    public SubjectUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên môn học không được để trống")
            .MaximumLength(200).WithMessage("Tên môn học không được vượt quá 200 ký tự");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã môn học không được để trống")
            .MaximumLength(50).WithMessage("Mã môn học không được vượt quá 50 ký tự")
            .Matches(@"^[A-Z0-9_]+$").WithMessage("Mã môn học chỉ được chứa chữ in hoa, số và dấu gạch dưới");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}