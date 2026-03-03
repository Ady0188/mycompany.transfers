using FluentValidation;
using MyCompany.Transfers.Application.Rates.Commands;

namespace MyCompany.Transfers.Application.Rates.Validators;

public sealed class UpsertFxRateCommandValidator : AbstractValidator<UpsertFxRateCommand>
{
    public UpsertFxRateCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.BaseCurrency).NotEmpty().WithMessage("BaseCurrency обязателен.").MaximumLength(3);
        RuleFor(x => x.QuoteCurrency).NotEmpty().WithMessage("QuoteCurrency обязателен.").MaximumLength(3);
        RuleFor(x => x.Rate).GreaterThan(0).WithMessage("Курс должен быть положительным.");
        RuleFor(x => x.Source).MaximumLength(32);
    }
}
