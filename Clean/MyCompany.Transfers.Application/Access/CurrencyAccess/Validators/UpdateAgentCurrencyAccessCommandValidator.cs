using FluentValidation;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Commands;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Validators;

public sealed class UpdateAgentCurrencyAccessCommandValidator : AbstractValidator<UpdateAgentCurrencyAccessCommand>
{
    public UpdateAgentCurrencyAccessCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency обязателен.");
    }
}
