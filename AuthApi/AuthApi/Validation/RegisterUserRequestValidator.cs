using AuthApi.API.Dtos;
using FluentValidation;

namespace AuthApi.API.Validation;

public sealed class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address format.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");
        
        RuleFor(x => x.Role).IsInEnum().WithMessage("Invalid role specified.");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
    }
}