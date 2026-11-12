using Bookmarks.Api.Data;
using Bookmarks.Api.Diagnostics;
using Bookmarks.Api.Extensions;
using Bookmarks.Api.Middleware;
using Bookmarks.Api.Services;
using Bookmarks.Api.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Shared.Observability;

const string WebCorsPolicy = "web";

var builder = WebApplication.CreateBuilder(args);

// All telemetry, in one call. Everything it needs comes from the Observability section and
// the standard OTEL_EXPORTER_OTLP_* keys in appsettings. No backend is named in code.
builder.AddSharedObservability(observability =>
{
    observability.ActivitySources.Add(ApplicationDiagnostics.ServiceName);
    observability.Meters.Add(ApplicationDiagnostics.ServiceName);
    observability.ActivitySources.Add(ApplicationDiagnostics.NpgsqlActivitySourceName);
});

builder.Services.AddCors(options => options.AddPolicy(WebCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyMethod()
    // baggage carries the caller's action name. Without it listed here the browser's
    // preflight fails and the header never arrives.
    .WithHeaders("content-type", "baggage", "traceparent")
    // A browser cannot read a response header cross-origin unless it is exposed.
    .WithExposedHeaders(TraceIdResponseMiddleware.HeaderName)));

builder.Services.AddControllers(options => options.ReturnHttpNotAcceptable = true);
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(
        builder.Configuration.GetConnectionString("bookmarksdb"),
        npgsql => npgsql.MigrationsHistoryTable(
            HistoryRepository.DefaultTableName, Schemas.Application))
    .UseSnakeCaseNamingConvention());

builder.Services.AddValidatorsFromAssemblyContaining<SaveBookmarkDtoValidator>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.AddScoped<IBookmarkTagService, BookmarkTagService>();
builder.Services.AddScoped<ITagService, TagService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(WebCorsPolicy);
app.UseMiddleware<TraceIdResponseMiddleware>();

// Mapped in every environment: a load balancer or container scheduler needs this in
// production, and it reports only healthy or unhealthy, never which dependencies exist.
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.ApplyMigrationsAsync();
}

app.MapControllers();

await app.RunAsync();
