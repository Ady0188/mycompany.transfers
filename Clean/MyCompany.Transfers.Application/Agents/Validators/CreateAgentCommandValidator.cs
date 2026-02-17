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
    }
}
