using AuthApi.API.Dtos;
using FluentValidation;

namespace AuthApi.API.Validation;

public sealed class RefreshTokenRequestValidator  : AbstractValidator<LogOutUserRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Token is required");
    }
}