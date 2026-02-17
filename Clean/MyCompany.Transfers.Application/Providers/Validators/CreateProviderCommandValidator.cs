using FluentValidation;
using MyCompany.Transfers.Application.Providers.Commands;

namespace MyCompany.Transfers.Application.Providers.Validators;

public sealed class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name обязателен.").MaximumLength(128);
        RuleFor(x => x.BaseUrl).NotEmpty().WithMessage("BaseUrl обязателен.").MaximumLength(512);
        RuleFor(x => x.TimeoutSeconds).InclusiveBetween(1, 300).WithMessage("TimeoutSeconds должен быть от 1 до 300.");
        RuleFor(x => x.FeePermille).InclusiveBetween(0, 10000).WithMessage("FeePermille должен быть от 0 до 10000.");
    }
}
