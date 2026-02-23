using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MyCompany.Transfers.Admin.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

// HttpClient и API-клиент — нужны и на сервере (Host), и в браузере (WASM)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddHttpClient("Api", (sp, client) =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
}).AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddSingleton<ITimeZoneListService, TimeZoneListService>();
builder.Services.AddScoped<IAgentsApiService, AgentsApiService>();
builder.Services.AddScoped<IProvidersApiService, ProvidersApiService>();
builder.Services.AddScoped<IServicesApiService, ServicesApiService>();
builder.Services.AddScoped<IAccountDefinitionsApiService, AccountDefinitionsApiService>();
builder.Services.AddScoped<ITerminalsApiService, TerminalsApiService>();

await builder.Build().RunAsync();
