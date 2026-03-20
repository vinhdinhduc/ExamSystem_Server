using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không hợp lệ")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(6).WithMessage("Mật khẩu tối thiểu 6 ký tự")
            .MaximumLength(50).WithMessage("Mật khẩu không được vượt quá 50 ký tự");

        RuleFor(x => x.Username)
            .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự")
            .When(x => x.Username != null);

        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự")
            .When(x => x.FullName != null);
    }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("Tên đăng nhập hoặc email không được để trống");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống");
    }
}

public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token không được để trống");
    }
}

public class LogoutDtoValidator : AbstractValidator<LogoutDto>
{
    public LogoutDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token không được để trống");
    }
}
