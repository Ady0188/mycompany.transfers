using FluentValidation;
using MyCompany.Transfers.Application.AccountDefinitions.Commands;

namespace MyCompany.Transfers.Application.AccountDefinitions.Validators;

public sealed class DeleteAccountDefinitionCommandValidator : AbstractValidator<DeleteAccountDefinitionCommand>
{
    public DeleteAccountDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.");
    }
}
