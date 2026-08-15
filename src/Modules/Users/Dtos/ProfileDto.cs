using FluentValidation;
using Shared.Infra.Enums;

namespace Users.Dtos;

// request body dto
public record CreateProfileRequest(
    string firstName, 
    string lastName, 
    string phone,
    string? gender
);

public record UpdateProfileRequest(
    string? FirstName, 
    string? LastName, 
    string? Phone,
    string? Gender
);

// response dto
public record ProfileRespone(
    string firstName, 
    string lastName, 
    string phone,
    string gender
);

// validation rule
public class CreateProfileRequestValidator: AbstractValidator<CreateProfileRequest>
{
    public CreateProfileRequestValidator()
    {
        RuleFor(x => x.firstName)
        .NotEmpty().WithMessage("This field is required")
        .MaximumLength(100).WithMessage("Not more than 100 chars");

        RuleFor(x => x.lastName)
        .NotEmpty().WithMessage("This field is required")
        .MaximumLength(100).WithMessage("Not more than 100 chars");

        RuleFor(x => x.phone)
        .NotEmpty().WithMessage("This field is required")
        .Matches(@"^\+?[0-9]{7,15}$")
        .MaximumLength(20).WithMessage("Not more than 100 chars");

        RuleFor(x => x.gender)
        .IsEnumName(typeof(Gender), caseSensitive: false)
        .When(x => !string.IsNullOrWhiteSpace(x.gender))
        .WithMessage($"Invalid gender. Acceptable values are: {string.Join(", ", Enum.GetNames(typeof(Gender)))}");;
    }
}

public class UpdateProfileRequestValidator: AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x=>x.FirstName)
        .MaximumLength(100).WithMessage("Must be 100 chars")
        .When(x=>!string.IsNullOrWhiteSpace(x.FirstName));

        RuleFor(x=>x.LastName)
        .MaximumLength(100).WithMessage("Must be 100 chars")
        .When(x=>!string.IsNullOrWhiteSpace(x.LastName));

        RuleFor(x=>x.Phone)
        .MaximumLength(20).WithMessage("Must be 20 chars")
        .Matches(@"^\+?[0-9]{7,15}$")
        .When(x=>!string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x=>x.Gender)
        .IsEnumName(typeof(Gender), caseSensitive: false).WithMessage("Invalid gender")
        .When(x=>!string.IsNullOrWhiteSpace(x.Gender));
    }
}