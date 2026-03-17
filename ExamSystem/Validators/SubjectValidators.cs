using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class SubjectCreateDtoValidator : AbstractValidator<SubjectCreateDto>
{
    public SubjectCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters")
            .Matches(@"^[A-Z0-9_]+$").WithMessage("Code must contain only uppercase letters, numbers, and underscores");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public class SubjectUpdateDtoValidator : AbstractValidator<SubjectUpdateDto>
{
    public SubjectUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters")
            .Matches(@"^[A-Z0-9_]+$").WithMessage("Code must contain only uppercase letters, numbers, and underscores");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}