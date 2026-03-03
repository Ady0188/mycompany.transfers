using FluentValidation;
using MyCompany.Transfers.Application.Agents.Commands;

namespace MyCompany.Transfers.Application.Agents.Validators;

public sealed class CreateAgentCommandValidator : AbstractValidator<CreateAgentCommand>
{
    public CreateAgentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id обязателен.")
            .MaximumLength(64);
        RuleFor(x => x.Account)
            .NotEmpty().WithMessage("Счёт обязателен для проводок.")
            .MaximumLength(128);
        RuleFor(x => x.Locale)
            .NotEmpty().WithMessage("Locale обязателен.")
            .Must(l => l is "ru" or "en")
            .WithMessage("Locale должен быть 'ru' или 'en'.");
    }
}
