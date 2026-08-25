using AuthApi.API.Dtos;
using FluentValidation;

namespace AuthApi.API.Validation;

public sealed class TemporaryCodeValidator : AbstractValidator<TemporaryCode>
{
    public TemporaryCodeValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required");
    }
}