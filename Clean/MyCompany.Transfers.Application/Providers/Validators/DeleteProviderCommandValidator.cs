using FluentValidation;
using MyCompany.Transfers.Application.Providers.Commands;

namespace MyCompany.Transfers.Application.Providers.Validators;

public sealed class DeleteProviderCommandValidator : AbstractValidator<DeleteProviderCommand>
{
    public DeleteProviderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(64);
    }
}
