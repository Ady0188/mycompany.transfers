using FluentValidation;
using MyCompany.Transfers.Application.Parameters.Commands;

namespace MyCompany.Transfers.Application.Parameters.Validators;

public sealed class CreateParameterCommandValidator : AbstractValidator<CreateParameterCommand>
{
    public CreateParameterCommandValidator()
    {
        RuleFor(x => x.Id)
            .MaximumLength(64).When(x => !string.IsNullOrEmpty(x.Id))
            .WithMessage("Id не более 64 символов.");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Код обязателен.").MaximumLength(64);
    }
}
