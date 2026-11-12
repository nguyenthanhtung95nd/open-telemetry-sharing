using Inventory.Api.Data;
using Inventory.Api.Diagnostics;
using Inventory.Api.Logging;
using Inventory.Api.Messaging;
using Inventory.Api.Repositories;
using Inventory.Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Trace;
using Shared.Messaging;
using Shared.Observability;
using Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("InventoryDb")));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();

var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq must be configured.");
builder.Services.AddSingleton<IRabbitMqPublisher>(new RabbitMqPublisher(rabbitMqConnectionString));
builder.Services.AddSingleton<IInventoryEventsPublisher, InventoryEventsPublisher>();

builder.Services.AddSingleton<IEventHandler<OrderCreatedEvent>, OrderCreatedHandler>();
builder.Services.AddHostedService<OrderCreatedConsumerHostedService>();

builder.Services.AddSingleton<IEventHandler<PaymentFailedEvent>, PaymentFailedHandler>();
builder.Services.AddHostedService<PaymentFailedConsumerHostedService>();

builder.AddKeycloakAuthentication();

builder.AddSharedObservability(observability =>
{
    observability.ActivitySources.Add(RabbitMqDiagnostics.ActivitySourceName);
    observability.ActivitySources.Add(ApplicationDiagnostics.ActivitySource.Name);
    observability.Meters.Add(ApplicationDiagnostics.Meter.Name);
    observability.Meters.Add(MessagingDiagnostics.MeterName);
    observability.Baggage.AllowedKeyPrefixes.Add(OrderPipelineTags.BaggagePrefix);

    foreach (var eventId in LogEventIds.Audited) observability.Audit.EventIds.Add(eventId);
});

// Npgsql ships its own tracing hook, which only this service's own package reference can reach.
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddNpgsql());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
