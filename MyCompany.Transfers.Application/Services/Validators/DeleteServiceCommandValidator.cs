using FluentValidation;
using MyCompany.Transfers.Application.Services.Commands;

namespace MyCompany.Transfers.Application.Services.Validators;

public sealed class DeleteServiceCommandValidator : AbstractValidator<DeleteServiceCommand>
{
    public DeleteServiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(128);
    }
}
