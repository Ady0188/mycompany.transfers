using FluentValidation;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Queries;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Validators;

public sealed class GetAgentCurrencyAccessByKeyQueryValidator : AbstractValidator<GetAgentCurrencyAccessByKeyQuery>
{
    public GetAgentCurrencyAccessByKeyQueryValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency обязателен.");
    }
}
