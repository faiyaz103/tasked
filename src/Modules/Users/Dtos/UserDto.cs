using FluentValidation;
using Shared.Infra.Enums;

namespace Users.Dtos;

public record CreateUserRequest(
    string Email,
    string? Role,
    string Password,
    string ConfirmPassword
);

public record CreateUserResponse(
    string Message
);

public record SignInUserRequest(
    string Email,
    string Password
);

public record TokenResponse(
    string AccessToken,
    string RefreshToken
);

public class CreateUserRequestValidator: AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x=>x.Email)
        .NotEmpty().WithMessage("This field is required")
        .MaximumLength(100).WithMessage("Not more than 100 chars");

        RuleFor(x => x.Password)
        .NotEmpty().WithMessage("Password is required.")
        .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
        .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
        .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.ConfirmPassword)
        .Equal(x => x.Password).WithMessage("Passwords do not match.");

        // Validate the Role ONLY if it was provided
        RuleFor(x => x.Role)
        .IsEnumName(typeof(Roles), caseSensitive: false)
        .When(x => !string.IsNullOrWhiteSpace(x.Role))
        .WithMessage($"Invalid role. Acceptable values are: {string.Join(", ", Enum.GetNames(typeof(Roles)))}");
    }
}

public class CreateUserSignInRequestValidator: AbstractValidator<SignInUserRequest>
{
    public CreateUserSignInRequestValidator()
    {
        RuleFor(x=>x.Email)
        .NotEmpty().WithMessage("This field is required")
        .MaximumLength(100).WithMessage("Not more than 100 chars");

        RuleFor(x => x.Password)
        .NotEmpty().WithMessage("Password is required.")
        .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
        .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
        .Matches("[0-9]").WithMessage("Password must contain at least one number.");
    }
}