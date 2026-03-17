using ExamSystem.DTOs;
using FluentValidation;

namespace ExamSystem.Validators;

public class ExamCreateDtoValidator : AbstractValidator<ExamCreateDto>
{
    public ExamCreateDtoValidator()
    {
        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage("SubjectId must be greater than 0");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("CreatedByUserId is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Instructions)
            .MaximumLength(2000).WithMessage("Instructions must not exceed 2000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Instructions));

        RuleFor(x => x.Duration)
            .GreaterThan(0).WithMessage("Duration must be greater than 0");

        RuleFor(x => x.TotalQuestions)
            .GreaterThan(0).WithMessage("TotalQuestions must be greater than 0");

        RuleFor(x => x.PassScore)
            .GreaterThanOrEqualTo(0).WithMessage("PassScore must be greater than or equal to 0");

        RuleFor(x => x.MaxAttempts)
            .GreaterThan(0).WithMessage("MaxAttempts must be greater than 0");

        RuleFor(x => x.Status)
            .InclusiveBetween((byte)0, (byte)2).WithMessage("Status is invalid");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
            .WithMessage("EndDate must be greater than or equal to StartDate");

        RuleFor(x => x.AccessCode)
            .MaximumLength(50).WithMessage("AccessCode must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.AccessCode));
    }
}

public class ExamUpdateDtoValidator : AbstractValidator<ExamUpdateDto>
{
    public ExamUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Instructions)
            .MaximumLength(2000).WithMessage("Instructions must not exceed 2000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Instructions));

        RuleFor(x => x.Duration)
            .GreaterThan(0).WithMessage("Duration must be greater than 0");

        RuleFor(x => x.TotalQuestions)
            .GreaterThan(0).WithMessage("TotalQuestions must be greater than 0");

        RuleFor(x => x.PassScore)
            .GreaterThanOrEqualTo(0).WithMessage("PassScore must be greater than or equal to 0");

        RuleFor(x => x.MaxAttempts)
            .GreaterThan(0).WithMessage("MaxAttempts must be greater than 0");

        RuleFor(x => x.Status)
            .InclusiveBetween((byte)0, (byte)2).WithMessage("Status is invalid");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
            .WithMessage("EndDate must be greater than or equal to StartDate");

        RuleFor(x => x.AccessCode)
            .MaximumLength(50).WithMessage("AccessCode must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.AccessCode));
    }
}

public class ExamQuestionCreateDtoValidator : AbstractValidator<ExamQuestionCreateDto>
{
    public ExamQuestionCreateDtoValidator()
    {
        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("QuestionId is required");

        RuleFor(x => x.OrderIndex)
            .GreaterThan(0).WithMessage("OrderIndex must be greater than 0");

        RuleFor(x => x.Score)
            .GreaterThan(0).WithMessage("Score must be greater than 0");
    }
}

public class ReorderExamQuestionsDtoValidator : AbstractValidator<ReorderExamQuestionsDto>
{
    public ReorderExamQuestionsDtoValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items is required")
            .Must(items => items.Count > 0).WithMessage("Items must not be empty");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ExamQuestionId)
                .GreaterThan(0).WithMessage("ExamQuestionId must be greater than 0");

            item.RuleFor(i => i.OrderIndex)
                .GreaterThan(0).WithMessage("OrderIndex must be greater than 0");
        });
    }
}

public class PublishExamDtoValidator : AbstractValidator<PublishExamDto>
{
    public PublishExamDtoValidator()
    {
        RuleFor(x => x.PublishedByUserId)
            .NotEmpty().WithMessage("PublishedByUserId is required");
    }
}

public class ExamAssignmentCreateDtoValidator : AbstractValidator<ExamAssignmentCreateDto>
{
    public ExamAssignmentCreateDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || x.GroupId.HasValue)
            .WithMessage("Must assign exam to user or group");

        RuleFor(x => x.GroupId)
            .GreaterThan(0).When(x => x.GroupId.HasValue)
            .WithMessage("GroupId must be greater than 0");

        RuleFor(x => x)
            .Must(x => !(x.UserId.HasValue && x.GroupId.HasValue))
            .WithMessage("Only one target assignment is allowed: user or group");
    }
}
