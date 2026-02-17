using FluentValidation;
using MyCompany.Transfers.Application.Terminals.Queries;

namespace MyCompany.Transfers.Application.Terminals.Validators;

public sealed class GetTerminalByIdQueryValidator : AbstractValidator<GetTerminalByIdQuery>
{
    public GetTerminalByIdQueryValidator()
    {
        RuleFor(x => x.TerminalId).NotEmpty().WithMessage("Id терминала обязателен.").MaximumLength(64);
    }
}
