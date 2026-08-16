using Edp.Template.Api.Models;
using FluentValidation;

namespace Edp.Template.Api.Validators;

public sealed class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(250);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Code may only contain letters, numbers, hyphens and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
