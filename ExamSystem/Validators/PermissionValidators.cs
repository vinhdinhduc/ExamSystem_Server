using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class PermissionCreateDtoValidator : AbstractValidator<PermissionCreateDto>
{
    public PermissionCreateDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã quyền không được để trống")
            .MaximumLength(100).WithMessage("Mã quyền không được vượt quá 100 ký tự")
            .Matches(@"^[A-Z_]+$").WithMessage("Mã quyền chỉ được chứa chữ in hoa và dấu gạch dưới");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class PermissionUpdateDtoValidator : AbstractValidator<PermissionUpdateDto>
{
    public PermissionUpdateDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã quyền không được để trống")
            .MaximumLength(100).WithMessage("Mã quyền không được vượt quá 100 ký tự")
            .Matches(@"^[A-Z_]+$").WithMessage("Mã quyền chỉ được chứa chữ in hoa và dấu gạch dưới");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
