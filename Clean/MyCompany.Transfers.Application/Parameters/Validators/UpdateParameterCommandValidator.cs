using FluentValidation;
using MyCompany.Transfers.Application.Parameters.Commands;

namespace MyCompany.Transfers.Application.Parameters.Validators;

public sealed class UpdateParameterCommandValidator : AbstractValidator<UpdateParameterCommand>
{
    public UpdateParameterCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(64);
    }
}
