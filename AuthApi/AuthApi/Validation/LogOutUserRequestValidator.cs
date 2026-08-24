using AuthApi.API.Dtos;
using FluentValidation;

namespace AuthApi.API.Validation;

public sealed class LogOutUserRequestValidator : AbstractValidator<LogOutUserRequest>
{
    public LogOutUserRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required");
    }
}