using FluentValidation;
using MyCompany.Transfers.Application.Agents.Queries;

namespace MyCompany.Transfers.Application.Agents.Validators;

public sealed class GetAgentByIdQueryValidator : AbstractValidator<GetAgentByIdQuery>
{
    public GetAgentByIdQueryValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty().WithMessage("Id агента обязателен.").MaximumLength(64);
    }
}
