using MyCompany.Transfers.Api.Helpers;
using MyCompany.Transfers.Application;
using MyCompany.Transfers.Infrastructure;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Encoding.RegisterProvider(new Windows1251EncodingProvider());

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
    {
        options.OutputFormatters.Insert(0, new CustomXmlSerializerOutputFormatter());
    })
    .AddXmlSerializerFormatters();

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
//builder.Services.AddTransient<SignatureMiddleware>();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("transfers", new() { Title = "MyCompany Transfers API", Version = "v1" });
    c.SwaggerDoc("tillabuy", new() { Title = "Tillabuy API", Version = "v1" });

    // фильтруем по GroupName, который вы задали в атрибутах
    c.DocInclusionPredicate((doc, api) =>
    {
        var group = api.GroupName ?? string.Empty;
        return (doc == "transfers" && group.Equals("transfers", StringComparison.OrdinalIgnoreCase))
            || (doc == "tillabuy" && group.Equals("tillabuy", StringComparison.OrdinalIgnoreCase));
    });
});

builder.Logging.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Mvc.ModelBinding", LogLevel.Debug);

//builder.Services.AddScoped<CoreProblemExceptionFilter>();

var app = builder.Build();

app.MapWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/mycompany/transfers/v111"),
    branch =>
    {
        branch.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch
            {
                var problem = Results.Problem(
                    context.TraceIdentifier);

                await problem.ExecuteAsync(context);
            }
        });

        //branch.MapControllers(); // контроллеры под этим префиксом
    });

app.MapWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/mycompany/transfers/v12"),
    branch =>
    {
        branch.Use(async (context, next) =>
        {
            var originalBody = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await next();
            }
            catch (Exception ex)
            {
                var raw = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();

                ProblemDetails? problem = null;
                try { problem = JsonSerializer.Deserialize<ProblemDetails>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                catch { /* ignore */ }

                var message = problem?.Detail
                    ?? problem?.Title
                    ?? $"Request failed with status {context.Response.StatusCode}.";

                // ❶ Любое необработанное исключение → наш формат 500
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var custom500 = new
                {
                    success = false,
                    status = 500,
                    code = "UNEXPECTED_ERROR",
                    message = "An unhandled exception occurred.",
                    traceId = context.TraceIdentifier
                };

                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, custom500);
                await context.Response.Body.FlushAsync();

                // Восстанавливаем поток и выходим
                context.Response.Body.Position = 0;
                await context.Response.Body.CopyToAsync(originalBody);
                context.Response.Body = originalBody;
                return;
            }

            // ❷ Ответ без исключения
            buffer.Position = 0;

            if (context.Response.StatusCode >= 400)
            {
                // читаем исходное тело (может быть ProblemDetails или что угодно)
                var raw = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();

                // пробуем понять, что это было, чтобы красивее смэпить (опционально)
                ProblemDetails? problem = null;
                try { problem = JsonSerializer.Deserialize<ProblemDetails>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                catch { /* ignore */ }

                // Маппинг в ваш единый формат
                var custom = MapToCustomError(context, problem, raw);

                context.Response.Clear();
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(custom);
                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
            else
            {
                // ❸ 2xx — просто отдать как есть
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
            }

            context.Response.Body = originalBody;
        });

        //branch.MapControllers();
    });

static object MapToCustomError(HttpContext ctx, ProblemDetails? problem, string rawFallback)
{
    var status = ctx.Response.StatusCode;

    // Подберите свои коды под статусы/типы ошибок
    var code = status switch
    {
        400 => "VALIDATION_ERROR",
        401 => "UNAUTHORIZED",
        403 => "FORBIDDEN",
        404 => "NOT_FOUND",
        409 => "CONFLICT",
        422 => "UNPROCESSABLE",
        _ => "ERROR"
    };

    // базовое сообщение
    var message = problem?.Detail
        ?? problem?.Title
        ?? $"Request failed with status {status}.";

    // можно пробросить детали валидации, если есть
    object? details = null;
    if (problem is Microsoft.AspNetCore.Mvc.ValidationProblemDetails vpd)
    {
        details = vpd.Errors; // { "field": [ "error1", "error2" ], ... }
    }

    return new
    {
        success = false,
        status,
        code,
        message,
        details,                 // null если нет
        traceId = ctx.TraceIdentifier
    };
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    //await ProviderSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/transfers/swagger.json", "MyCompany Transfers API v1");
        c.SwaggerEndpoint("/swagger/tillabuy/swagger.json", "Tillabuy API v1");
    });
}

//app.UseMiddleware<SignatureMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
