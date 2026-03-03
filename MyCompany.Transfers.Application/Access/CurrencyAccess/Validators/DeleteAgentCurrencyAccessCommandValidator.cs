using FluentValidation;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Commands;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Validators;

public sealed class DeleteAgentCurrencyAccessCommandValidator : AbstractValidator<DeleteAgentCurrencyAccessCommand>
{
    public DeleteAgentCurrencyAccessCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency обязателен.");
    }
}
