using FluentValidation;
using MyCompany.Transfers.Application.Services.Commands;

namespace MyCompany.Transfers.Application.Services.Validators;

public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Id).MaximumLength(128).When(x => !string.IsNullOrEmpty(x.Id));
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage("ProviderId обязателен.").MaximumLength(64);
        RuleFor(x => x.AccountDefinitionId).NotEqual(Guid.Empty).WithMessage("AccountDefinitionId обязателен.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name обязателен.").MaximumLength(128);
        RuleFor(x => x.AllowedCurrencies).NotNull();
        RuleFor(x => x.MinAmountMinor).GreaterThanOrEqualTo(0).WithMessage("MinAmountMinor не может быть отрицательным.");
        RuleFor(x => x.MaxAmountMinor).GreaterThanOrEqualTo(0).WithMessage("MaxAmountMinor не может быть отрицательным.");
        RuleFor(x => x.MaxAmountMinor).GreaterThanOrEqualTo(x => x.MinAmountMinor).WithMessage("MaxAmountMinor должен быть не меньше MinAmountMinor.");
    }
}
