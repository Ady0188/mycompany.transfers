using FluentValidation;
using MyCompany.Transfers.Application.Transfers.Commands;

namespace MyCompany.Transfers.Application.Transfers.Validators;

public sealed class PrepareCommandValidator : AbstractValidator<PrepareCommand>
{
    public PrepareCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.TerminalId).NotEmpty();
        RuleFor(x => x.ExternalId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Account).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
