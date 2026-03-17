using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class RoleCreateDtoValidator : AbstractValidator<RoleCreateDto>
{
    public RoleCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class RoleUpdateDtoValidator : AbstractValidator<RoleUpdateDto>
{
    public RoleUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class AssignPermissionsToRoleDtoValidator : AbstractValidator<AssignPermissionsToRoleDto>
{
    public AssignPermissionsToRoleDtoValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("PermissionIds is required");
    }
}
