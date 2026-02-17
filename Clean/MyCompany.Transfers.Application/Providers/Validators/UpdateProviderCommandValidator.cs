using FluentValidation;
using MyCompany.Transfers.Application.Providers.Commands;

namespace MyCompany.Transfers.Application.Providers.Validators;

public sealed class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(64);
        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name не может быть пустым.").MaximumLength(128));
        When(x => x.BaseUrl is not null, () =>
            RuleFor(x => x.BaseUrl).NotEmpty().WithMessage("BaseUrl не может быть пустым.").MaximumLength(512));
        When(x => x.TimeoutSeconds.HasValue, () =>
            RuleFor(x => x.TimeoutSeconds!.Value).InclusiveBetween(1, 300).WithMessage("TimeoutSeconds должен быть от 1 до 300."));
        When(x => x.FeePermille.HasValue, () =>
            RuleFor(x => x.FeePermille!.Value).InclusiveBetween(0, 10000).WithMessage("FeePermille должен быть от 0 до 10000."));
    }
}
