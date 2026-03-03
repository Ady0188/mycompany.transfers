using FluentValidation;
using MyCompany.Transfers.Application.AccountDefinitions.Queries;

namespace MyCompany.Transfers.Application.AccountDefinitions.Validators;

public sealed class GetAccountDefinitionByIdQueryValidator : AbstractValidator<GetAccountDefinitionByIdQuery>
{
    public GetAccountDefinitionByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id обязателен.");
    }
}
