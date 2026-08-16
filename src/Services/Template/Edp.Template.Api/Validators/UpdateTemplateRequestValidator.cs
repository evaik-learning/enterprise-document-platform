using Edp.Template.Api.Models;
using FluentValidation;

namespace Edp.Template.Api.Validators;

public sealed class UpdateTemplateRequestValidator : AbstractValidator<UpdateTemplateRequest>
{
    public UpdateTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(250);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required for concurrency checks.");
    }
}
