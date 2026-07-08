using FluentValidation;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Validation;

public class CreateSubjectRequestValidator : AbstractValidator<CreateSubjectRequest>
{
    public CreateSubjectRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(50);
        RuleFor(r => r.ColorHex).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$");
    }
}

public class UpdateSubjectRequestValidator : AbstractValidator<UpdateSubjectRequest>
{
    public UpdateSubjectRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(50);
        RuleFor(r => r.ColorHex).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$");
        RuleFor(r => r.SortOrder).GreaterThanOrEqualTo(0);
    }
}
