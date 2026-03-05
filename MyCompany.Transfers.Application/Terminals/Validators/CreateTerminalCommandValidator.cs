using FluentValidation;
using MyCompany.Transfers.Application.Terminals.Commands;

namespace MyCompany.Transfers.Application.Terminals.Validators;

public sealed class CreateTerminalCommandValidator : AbstractValidator<CreateTerminalCommand>
{
    public CreateTerminalCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.").MaximumLength(64);
        RuleFor(x => x.Account).NotEmpty().WithMessage("Счёт терминала обязателен.").MaximumLength(128);
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Валюта терминала обязательна.").Length(3).WithMessage("Валюта — 3 символа (ISO).");
        RuleFor(x => x.ApiKey).NotEmpty().WithMessage("ApiKey обязателен.").MaximumLength(128);
    }
}
