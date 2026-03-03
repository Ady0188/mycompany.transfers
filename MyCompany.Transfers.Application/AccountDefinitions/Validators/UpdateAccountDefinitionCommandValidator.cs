using FluentValidation;
using MyCompany.Transfers.Application.AccountDefinitions.Commands;

namespace MyCompany.Transfers.Application.AccountDefinitions.Validators;

public sealed class UpdateAccountDefinitionCommandValidator : AbstractValidator<UpdateAccountDefinitionCommand>
{
    public UpdateAccountDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.");
    }
}
