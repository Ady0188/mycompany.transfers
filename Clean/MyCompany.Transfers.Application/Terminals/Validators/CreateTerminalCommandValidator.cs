using FluentValidation;
using MyCompany.Transfers.Application.Terminals.Commands;

namespace MyCompany.Transfers.Application.Terminals.Validators;

public sealed class CreateTerminalCommandValidator : AbstractValidator<CreateTerminalCommand>
{
    public CreateTerminalCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.").MaximumLength(64);
        RuleFor(x => x.ApiKey).NotEmpty().WithMessage("ApiKey обязателен.").MaximumLength(128);
    }
}
