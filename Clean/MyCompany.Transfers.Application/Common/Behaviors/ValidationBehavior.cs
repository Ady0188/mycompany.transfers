using ErrorOr;
using FluentValidation;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;

namespace MyCompany.Transfers.Application.Common.Behaviors;

/// <summary>
/// Pipeline-поведение: выполняет валидацию запроса через FluentValidation перед вызовом обработчика.
/// При ошибках валидации выбрасывает <see cref="RequestValidationException"/> с списком <see cref="Error"/>.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators is null || !_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            if (result.Errors.Count > 0)
                failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .Select(f => AppErrors.Common.Validation(f.ErrorMessage, f.PropertyName))
            .Cast<Error>()
            .ToList();

        throw new RequestValidationException(errors);
    }
}
