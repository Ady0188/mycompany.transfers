using FluentValidation;
using MyCompany.Transfers.Application.Agents.Commands;

namespace MyCompany.Transfers.Application.Agents.Validators;

public sealed class UpdateAgentCommandValidator : AbstractValidator<UpdateAgentCommand>
{
    public UpdateAgentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.").MaximumLength(64);
        When(x => x.Account is not null, () =>
            RuleFor(x => x.Account).MaximumLength(128));
    }
}
