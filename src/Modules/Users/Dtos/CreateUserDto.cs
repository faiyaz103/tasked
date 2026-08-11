using FluentValidation;

namespace Users.Dtos;

// Request & Response Contracts
public record CreateUserRequest(string Name, string Email);

public record UserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);

// Validation Schema
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters.");
    }
}