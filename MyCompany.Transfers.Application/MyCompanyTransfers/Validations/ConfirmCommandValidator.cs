using FluentValidation;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.MyCompanyTransfers.Commands;

namespace MyCompany.Transfers.Application.MyCompanyTransfers.Validations;

public sealed class ConfirmCommandValidator : AbstractValidator<ConfirmCommand>
{
    public ConfirmCommandValidator(ITransferRepository transfers)
    {
        RuleFor(x => x.ExternalId).NotEmpty();
        RuleFor(x => x.QuotationId).NotEmpty();
    }
}