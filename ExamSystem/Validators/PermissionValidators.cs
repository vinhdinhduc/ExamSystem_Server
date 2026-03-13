using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class PermissionCreateDtoValidator : AbstractValidator<PermissionCreateDto>
{
    public PermissionCreateDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(100).WithMessage("Code must not exceed 100 characters")
            .Matches(@"^[A-Z_]+$").WithMessage("Code must contain only uppercase letters and underscores");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class PermissionUpdateDtoValidator : AbstractValidator<PermissionUpdateDto>
{
    public PermissionUpdateDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(100).WithMessage("Code must not exceed 100 characters")
            .Matches(@"^[A-Z_]+$").WithMessage("Code must contain only uppercase letters and underscores");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
