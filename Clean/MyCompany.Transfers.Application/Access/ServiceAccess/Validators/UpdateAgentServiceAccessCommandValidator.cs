using FluentValidation;
using MyCompany.Transfers.Application.Access.ServiceAccess.Commands;

namespace MyCompany.Transfers.Application.Access.ServiceAccess.Validators;

public sealed class UpdateAgentServiceAccessCommandValidator : AbstractValidator<UpdateAgentServiceAccessCommand>
{
    public UpdateAgentServiceAccessCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage("ServiceId обязателен.");
    }
}
