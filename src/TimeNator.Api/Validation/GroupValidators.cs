using FluentValidation;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Validation;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(60);
        RuleFor(r => r.Description).MaximumLength(500);
        RuleFor(r => r.Password)
            .Must(p => p is { Length: >= 4 and <= 64 })
            .When(r => !r.IsPublic)
            .WithMessage("A private group needs a password of 4 to 64 characters.");
    }
}

public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(60);
        RuleFor(r => r.Description).MaximumLength(500);
        RuleFor(r => r.Announcement).MaximumLength(500);
        RuleFor(r => r.MinDailySeconds).InclusiveBetween(60, 24 * 60 * 60).When(r => r.MinDailySeconds is not null);
    }
}
