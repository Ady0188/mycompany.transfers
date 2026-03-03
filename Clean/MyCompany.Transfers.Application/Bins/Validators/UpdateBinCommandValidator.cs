using FluentValidation;
using MyCompany.Transfers.Application.Bins.Commands;

namespace MyCompany.Transfers.Application.Bins.Validators;

public sealed class UpdateBinCommandValidator : AbstractValidator<UpdateBinCommand>
{
    public UpdateBinCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Prefix).NotEmpty().WithMessage("Префикс (бин) обязателен.").MaximumLength(32);
        RuleFor(x => x.Code).NotEmpty().WithMessage("Код обязателен.").MaximumLength(64)
            .Matches("^[A-Za-z]+$").WithMessage("Код допускает только латинские буквы.");
        RuleFor(x => x.Name).MaximumLength(256);
    }
}
