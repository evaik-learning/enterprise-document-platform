using Edp.Template.Api.Models;
using FluentValidation;

namespace Edp.Template.Api.Validators;

public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(250);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
