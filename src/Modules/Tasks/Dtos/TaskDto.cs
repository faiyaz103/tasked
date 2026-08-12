using FluentValidation;

namespace Tasks.Dtos;

public record CreateTaskRequest(
    string title
);

public record TaskResponse(
    string title,
    DateTimeOffset createAt
);

public class CreateTaskValidator: AbstractValidator<CreateTaskRequest>
{
    public CreateTaskValidator()
    {
        RuleFor(x=>x.title)
        .NotEmpty().WithMessage("This field is required")
        .MaximumLength(100).WithMessage("Not more than 100 chars");
    }
}