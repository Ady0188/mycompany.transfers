using FluentValidation;
using MyCompany.Transfers.Application.Agents.Commands;

namespace MyCompany.Transfers.Application.Agents.Validators;

public sealed class CreditAgentCommandValidator : AbstractValidator<CreditAgentCommand>
{
    public CreditAgentCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency обязателен.").MaximumLength(3);
        RuleFor(x => x.AmountMinor).GreaterThan(0).WithMessage("Сумма зачисления должна быть положительной.");
    }
}
