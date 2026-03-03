using FluentValidation;
using MyCompany.Transfers.Application.Rates.Queries;

namespace MyCompany.Transfers.Application.Rates.Validators;

public sealed class GetFxRateByKeyForAdminQueryValidator : AbstractValidator<GetFxRateByKeyForAdminQuery>
{
    public GetFxRateByKeyForAdminQueryValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.BaseCurrency).NotEmpty().WithMessage("BaseCurrency обязателен.");
        RuleFor(x => x.QuoteCurrency).NotEmpty().WithMessage("QuoteCurrency обязателен.");
    }
}
