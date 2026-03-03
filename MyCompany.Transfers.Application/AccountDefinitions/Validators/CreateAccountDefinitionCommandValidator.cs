using FluentValidation;
using MyCompany.Transfers.Application.AccountDefinitions.Commands;

namespace MyCompany.Transfers.Application.AccountDefinitions.Validators;

public sealed class CreateAccountDefinitionCommandValidator : AbstractValidator<CreateAccountDefinitionCommand>
{
    public CreateAccountDefinitionCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code обязателен.").MaximumLength(64);
    }
}
