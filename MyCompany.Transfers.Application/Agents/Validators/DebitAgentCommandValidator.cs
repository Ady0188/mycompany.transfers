using FluentValidation;
using MyCompany.Transfers.Application.Agents.Commands;

namespace MyCompany.Transfers.Application.Agents.Validators;

public sealed class DebitAgentCommandValidator : AbstractValidator<DebitAgentCommand>
{
    public DebitAgentCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency обязателен.").MaximumLength(3);
        RuleFor(x => x.AmountMinor).GreaterThan(0).WithMessage("Сумма списания должна быть положительной.");
        RuleFor(x => x.DocId).GreaterThan(0).WithMessage("DocId должен быть положительным числом.");
    }
}
