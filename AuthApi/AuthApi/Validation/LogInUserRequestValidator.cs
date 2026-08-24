using AuthApi.API.Dtos;
using FluentValidation;

namespace AuthApi.API.Validation;

public sealed class LogInUserRequestValidator : AbstractValidator<LogInUserRequest>
{
    public LogInUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}