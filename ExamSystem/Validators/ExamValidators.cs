using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class ExamCreateDtoValidator : AbstractValidator<ExamCreateDto>
{
    public ExamCreateDtoValidator()
    {
        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage("Mã môn học phải lớn hơn 0");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("Người tạo đề không được để trống");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề đề thi không được để trống")
            .MaximumLength(255).WithMessage("Tiêu đề đề thi không được vượt quá 255 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Instructions)
            .MaximumLength(2000).WithMessage("Hướng dẫn làm bài không được vượt quá 2000 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Instructions));

        RuleFor(x => x.Duration)
            .GreaterThan(0).WithMessage("Thời gian làm bài phải lớn hơn 0");

        RuleFor(x => x.TotalQuestions)
            .GreaterThan(0).WithMessage("Tổng số câu hỏi phải lớn hơn 0");

        RuleFor(x => x.PassScore)
            .GreaterThanOrEqualTo(0).WithMessage("Điểm đạt phải lớn hơn hoặc bằng 0");

        RuleFor(x => x.MaxAttempts)
            .GreaterThan(0).WithMessage("Số lần làm tối đa phải lớn hơn 0");

        RuleFor(x => x.Status)
            .InclusiveBetween((byte)0, (byte)2).WithMessage("Trạng thái không hợp lệ");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
            .WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu");

        RuleFor(x => x.AccessCode)
            .MaximumLength(50).WithMessage("Mã truy cập không được vượt quá 50 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.AccessCode));
    }
}

public class ExamUpdateDtoValidator : AbstractValidator<ExamUpdateDto>
{
    public ExamUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề đề thi không được để trống")
            .MaximumLength(255).WithMessage("Tiêu đề đề thi không được vượt quá 255 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Instructions)
            .MaximumLength(2000).WithMessage("Hướng dẫn làm bài không được vượt quá 2000 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Instructions));

        RuleFor(x => x.Duration)
            .GreaterThan(0).WithMessage("Thời gian làm bài phải lớn hơn 0");

        RuleFor(x => x.TotalQuestions)
            .GreaterThan(0).WithMessage("Tổng số câu hỏi phải lớn hơn 0");

        RuleFor(x => x.PassScore)
            .GreaterThanOrEqualTo(0).WithMessage("Điểm đạt phải lớn hơn hoặc bằng 0");

        RuleFor(x => x.MaxAttempts)
            .GreaterThan(0).WithMessage("Số lần làm tối đa phải lớn hơn 0");

        RuleFor(x => x.Status)
            .InclusiveBetween((byte)0, (byte)2).WithMessage("Trạng thái không hợp lệ");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
            .WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu");

        RuleFor(x => x.AccessCode)
            .MaximumLength(50).WithMessage("Mã truy cập không được vượt quá 50 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.AccessCode));
    }
}

public class ExamQuestionCreateDtoValidator : AbstractValidator<ExamQuestionCreateDto>
{
    public ExamQuestionCreateDtoValidator()
    {
        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("Câu hỏi không được để trống");

        RuleFor(x => x.OrderIndex)
            .GreaterThan(0)
            .When(x => x.OrderIndex.HasValue)
            .WithMessage("Thứ tự câu hỏi phải lớn hơn 0");

        RuleFor(x => x.Score)
            .GreaterThan(0)
            .When(x => x.Score.HasValue)
            .WithMessage("Điểm số phải lớn hơn 0");
    }
}

public class ReorderExamQuestionsDtoValidator : AbstractValidator<ReorderExamQuestionsDto>
{
    public ReorderExamQuestionsDtoValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Danh sách sắp xếp không được để trống")
            .Must(items => items.Count > 0).WithMessage("Danh sách sắp xếp không được rỗng");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ExamQuestionId)
                .GreaterThan(0).WithMessage("Mã câu hỏi trong đề phải lớn hơn 0");

            item.RuleFor(i => i.OrderIndex)
                .GreaterThan(0).WithMessage("Thứ tự câu hỏi phải lớn hơn 0");
        });
    }
}

public class PublishExamDtoValidator : AbstractValidator<PublishExamDto>
{
    public PublishExamDtoValidator()
    {
        RuleFor(x => x.PublishedByUserId)
            .NotEmpty().WithMessage("Người xuất bản không được để trống");
    }
}

public class ExamAssignmentCreateDtoValidator : AbstractValidator<ExamAssignmentCreateDto>
{
    public ExamAssignmentCreateDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || x.GroupId.HasValue)
            .WithMessage("Phải phân công đề thi cho người dùng hoặc nhóm");

        RuleFor(x => x.GroupId)
            .GreaterThan(0).When(x => x.GroupId.HasValue)
            .WithMessage("Mã nhóm phải lớn hơn 0");

        RuleFor(x => x)
            .Must(x => !(x.UserId.HasValue && x.GroupId.HasValue))
            .WithMessage("Chỉ được phân công cho một đối tượng: người dùng hoặc nhóm");
    }
}
