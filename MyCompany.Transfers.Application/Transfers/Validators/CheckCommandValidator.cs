using FluentValidation;
using MyCompany.Transfers.Application.Transfers.Commands;

namespace MyCompany.Transfers.Application.Transfers.Validators;

public sealed class CheckCommandValidator : AbstractValidator<CheckCommand>
{
    public CheckCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Account).NotEmpty();
    }
}
