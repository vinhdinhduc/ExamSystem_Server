using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class RoleCreateDtoValidator : AbstractValidator<RoleCreateDto>
{
    public RoleCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên vai trò không được để trống")
            .MaximumLength(100).WithMessage("Tên vai trò không được vượt quá 100 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class RoleUpdateDtoValidator : AbstractValidator<RoleUpdateDto>
{
    public RoleUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên vai trò không được để trống")
            .MaximumLength(100).WithMessage("Tên vai trò không được vượt quá 100 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class AssignPermissionsToRoleDtoValidator : AbstractValidator<AssignPermissionsToRoleDto>
{
    public AssignPermissionsToRoleDtoValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("Danh sách quyền không được để trống");
    }
}
