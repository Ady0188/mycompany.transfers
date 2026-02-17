using FluentValidation;
using MyCompany.Transfers.Application.Services.Commands;

namespace MyCompany.Transfers.Application.Services.Validators;

public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(128);
        When(x => x.MinAmountMinor.HasValue && x.MaxAmountMinor.HasValue, () =>
            RuleFor(x => x.MaxAmountMinor!.Value).GreaterThanOrEqualTo(x => x.MinAmountMinor!.Value)
                .WithMessage("MaxAmountMinor должен быть не меньше MinAmountMinor."));
    }
}
