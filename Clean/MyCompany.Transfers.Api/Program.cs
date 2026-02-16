using MyCompany.Transfers.Api.Helpers;
using MyCompany.Transfers.Application;
using MyCompany.Transfers.Infrastructure;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Host.UseNLog();

builder.Services.AddControllers(options =>
{
    options.OutputFormatters.Insert(0, new CustomXmlSerializerOutputFormatter());
})
    .AddXmlSerializerFormatters();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MyCompany Transfers API",
        Version = "v1",
        Description = "API для переводов (MyCompany, Tillabuy протоколы)"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyCompany Transfers API v1");
    });
}

app.MapControllers();
app.Run();
