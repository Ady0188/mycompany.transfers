using FluentValidation;
using MyCompany.Transfers.Application.Access.ServiceAccess.Queries;

namespace MyCompany.Transfers.Application.Access.ServiceAccess.Validators;

public sealed class GetAgentServiceAccessByKeyQueryValidator : AbstractValidator<GetAgentServiceAccessByKeyQuery>
{
    public GetAgentServiceAccessByKeyQueryValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("AgentId обязателен.");
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage("ServiceId обязателен.");
    }
}
