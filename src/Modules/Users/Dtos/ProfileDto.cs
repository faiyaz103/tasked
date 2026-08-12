using FluentValidation;
using Shared.Infra.Enums;

namespace Users.Dtos;

// request body dto
public record CreateProfileRequest(
    string firstName, 
    string lastName, 
    string phone,
    Gender gender
);

// response dto
public record ProfileRespone(
    string firstName, 
    string lastName, 
    string phone,
    Gender gender
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
        .MaximumLength(20).WithMessage("Not more than 100 chars");

        RuleFor(x => x.gender)
        .IsInEnum().WithMessage("Enter valid information");
    }
}