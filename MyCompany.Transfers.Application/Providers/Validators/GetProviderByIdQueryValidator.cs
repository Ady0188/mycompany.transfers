using FluentValidation;
using MyCompany.Transfers.Application.Providers.Queries;

namespace MyCompany.Transfers.Application.Providers.Validators;

public sealed class GetProviderByIdQueryValidator : AbstractValidator<GetProviderByIdQuery>
{
    public GetProviderByIdQueryValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage("Id провайдера обязателен.").MaximumLength(64);
    }
}
