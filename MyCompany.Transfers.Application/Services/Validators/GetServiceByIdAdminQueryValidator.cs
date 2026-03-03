using FluentValidation;
using MyCompany.Transfers.Application.Services.Queries;

namespace MyCompany.Transfers.Application.Services.Validators;

public sealed class GetServiceByIdAdminQueryValidator : AbstractValidator<GetServiceByIdAdminQuery>
{
    public GetServiceByIdAdminQueryValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage("Id услуги обязателен.").MaximumLength(128);
    }
}
