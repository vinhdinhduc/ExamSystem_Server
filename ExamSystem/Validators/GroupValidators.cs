using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class GroupCreateDtoValidator : AbstractValidator<GroupCreateDto>
{
    public GroupCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên nhóm không được để trống")
            .MaximumLength(100).WithMessage("Tên nhóm không được vượt quá 100 ký tự");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã nhóm không được để trống")
            .MaximumLength(50).WithMessage("Mã nhóm không được vượt quá 50 ký tự")
            .Matches("^[A-Z0-9_]+$").WithMessage("Mã nhóm chỉ được chứa chữ in hoa, số và dấu gạch dưới");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

   
    }
}

public class GroupUpdateDtoValidator : AbstractValidator<GroupUpdateDto>
{
    public GroupUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên nhóm không được để trống")
            .MaximumLength(100).WithMessage("Tên nhóm không được vượt quá 100 ký tự");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã nhóm không được để trống")
            .MaximumLength(50).WithMessage("Mã nhóm không được vượt quá 50 ký tự")
            .Matches("^[A-Z0-9_]+$").WithMessage("Mã nhóm chỉ được chứa chữ in hoa, số và dấu gạch dưới");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public class AddGroupMemberDtoValidator : AbstractValidator<AddGroupMemberDto>
{
    public AddGroupMemberDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Người dùng không được để trống");
    }
}
