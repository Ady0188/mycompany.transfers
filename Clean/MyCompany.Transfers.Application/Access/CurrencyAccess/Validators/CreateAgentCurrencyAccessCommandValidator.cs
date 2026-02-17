using FluentValidation;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Commands;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Validators;

public sealed class CreateAgentCurrencyAccessCommandValidator : AbstractValidator<CreateAgentCurrencyAccessCommand>
{
    public CreateAgentCurrencyAccessCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency обязателен.");
    }
}
