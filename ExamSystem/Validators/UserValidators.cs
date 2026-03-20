using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống")
            .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không hợp lệ")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(6).WithMessage("Mật khẩu tối thiểu 6 ký tự")
            .MaximumLength(50).WithMessage("Mật khẩu không được vượt quá 50 ký tự");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự");
    }
}

public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateDtoValidator()
    {
        When(x => x.Username != null, () =>
        {
            RuleFor(x => x.Username)
                .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự");
        });

        When(x => x.Email != null, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email không hợp lệ")
                .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự");
        });

        When(x => x.FullName != null, () =>
        {
            RuleFor(x => x.FullName)
                .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự");
        });
    }
}

public class UserChangePasswordDtoValidator : AbstractValidator<UserChangePasswordDto>
{
    public UserChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống")
            .MinimumLength(6).WithMessage("Mật khẩu mới tối thiểu 6 ký tự")
            .MaximumLength(50).WithMessage("Mật khẩu mới không được vượt quá 50 ký tự")
            .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại");
    }
}

public class AssignRolesToUserDtoValidator : AbstractValidator<AssignRolesToUserDto>
{
    public AssignRolesToUserDtoValidator()
    {
        RuleFor(x => x.RoleIds)
            .NotNull().WithMessage("Danh sách vai trò không được để trống")
            .Must(list => list.All(id => id != Guid.Empty))
            .WithMessage("Tất cả mã vai trò phải là GUID hợp lệ")
            .When(x => x.RoleIds != null && x.RoleIds.Count > 0);
    }
}
