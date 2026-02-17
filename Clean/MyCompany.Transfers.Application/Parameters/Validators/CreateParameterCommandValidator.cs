using FluentValidation;
using MyCompany.Transfers.Application.Parameters.Commands;

namespace MyCompany.Transfers.Application.Parameters.Validators;

public sealed class CreateParameterCommandValidator : AbstractValidator<CreateParameterCommand>
{
    public CreateParameterCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(64);
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code обязателен.").MaximumLength(64);
    }
}
