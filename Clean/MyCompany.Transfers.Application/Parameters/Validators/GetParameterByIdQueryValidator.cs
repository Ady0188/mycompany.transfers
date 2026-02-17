using FluentValidation;
using MyCompany.Transfers.Application.Parameters.Queries;

namespace MyCompany.Transfers.Application.Parameters.Validators;

public sealed class GetParameterByIdQueryValidator : AbstractValidator<GetParameterByIdQuery>
{
    public GetParameterByIdQueryValidator()
    {
        RuleFor(x => x.ParameterId).NotEmpty().WithMessage("Id параметра обязателен.").MaximumLength(64);
    }
}
