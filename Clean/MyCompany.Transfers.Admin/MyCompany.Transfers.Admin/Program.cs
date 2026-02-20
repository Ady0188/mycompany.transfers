using MudBlazor.Services;
using MyCompany.Transfers.Admin.Client.Pages;
using MyCompany.Transfers.Admin.Client.Services;
using MyCompany.Transfers.Admin.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();

// Авторизация: только логин через AD, токен в localStorage.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped<IAgentsApiService, AgentsApiService>();
builder.Services.AddScoped<IProvidersApiService, ProvidersApiService>();
builder.Services.AddHttpClient("Api", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["ApiBaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
}).AddHttpMessageHandler<AuthHeaderHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MyCompany.Transfers.Admin.Client._Imports).Assembly);

app.Run();
