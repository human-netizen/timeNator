using FluentValidation;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(r => r.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(r => r.DisplayName).NotEmpty().MaximumLength(50);
    }
}
