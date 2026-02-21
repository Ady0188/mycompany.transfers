using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MyCompany.Transfers.Admin.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped<IAgentsApiService, AgentsApiService>();
builder.Services.AddScoped<IProvidersApiService, ProvidersApiService>();

await builder.Build().RunAsync();
