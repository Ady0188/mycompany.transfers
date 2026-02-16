using FluentValidation;
using MyCompany.Transfers.Application.Transfers.Commands;

namespace MyCompany.Transfers.Application.Transfers.Validators;

public sealed class ConfirmCommandValidator : AbstractValidator<ConfirmCommand>
{
    public ConfirmCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.ExternalId).NotEmpty();
        RuleFor(x => x.QuotationId).NotEmpty();
    }
}
