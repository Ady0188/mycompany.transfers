using FluentValidation;
using MyCompany.Transfers.Application.Terminals.Commands;

namespace MyCompany.Transfers.Application.Terminals.Validators;

public sealed class DeleteTerminalCommandValidator : AbstractValidator<DeleteTerminalCommand>
{
    public DeleteTerminalCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(64);
    }
}
