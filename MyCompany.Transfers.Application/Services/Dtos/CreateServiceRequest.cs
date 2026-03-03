namespace MyCompany.Transfers.Application.Services.Dtos;

/// <summary>
/// Тело запроса создания услуги. Id не передаётся — генерируется на API (9-значное число).
/// </summary>
public sealed record CreateServiceRequest(string Name, string? Code);
