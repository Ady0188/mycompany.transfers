using FluentValidation;
using MyCompany.Transfers.Application.Agents.Commands;

namespace MyCompany.Transfers.Application.Agents.Validators;

public sealed class UpdateAgentCommandValidator : AbstractValidator<UpdateAgentCommand>
{
    public UpdateAgentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(64);
        When(x => x.Account is not null, () =>
            RuleFor(x => x.Account).MaximumLength(128));
        When(x => x.Locale is not null, () =>
            RuleFor(x => x.Locale!)
                .Must(l => l is "ru" or "en")
                .WithMessage("Locale должен быть 'ru' или 'en' при обновлении."));
    }
}
