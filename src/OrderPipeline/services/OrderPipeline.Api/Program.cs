using Destructurama;
using Npgsql;
using OpenTelemetry.Trace;
using OrderPipeline.Api.Clients;
using OrderPipeline.Api.Data;
using OrderPipeline.Api.Diagnostics;
using OrderPipeline.Api.Logging;
using OrderPipeline.Api.Messaging;
using OrderPipeline.Api.Repositories;
using OrderPipeline.Api.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Messaging;
using Shared.Observability;
using Shared.Security;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Destructure.UsingAttributes()
    .WriteTo.Console(),
    writeToProviders: true);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderPipelineDb")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

var inventoryApiUrl = builder.Configuration["InventoryApi:Url"]
    ?? throw new InvalidOperationException("InventoryApi:Url must be configured.");
builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
    client.BaseAddress = new Uri(inventoryApiUrl));

var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq must be configured.");
builder.Services.AddSingleton<IRabbitMqPublisher>(new RabbitMqPublisher(rabbitMqConnectionString));
builder.Services.AddSingleton<IOrderEventsPublisher, OrderEventsPublisher>();

builder.Services.AddSingleton<IEventHandler<StockReservedEvent>, StockReservedHandler>();
builder.Services.AddHostedService<StockReservedConsumerHostedService>();

builder.Services.AddSingleton<IEventHandler<StockReservationFailedEvent>, StockReservationFailedHandler>();
builder.Services.AddHostedService<StockReservationFailedConsumerHostedService>();

builder.Services.AddSingleton<IEventHandler<PaymentSucceededEvent>, PaymentSucceededHandler>();
builder.Services.AddHostedService<PaymentSucceededConsumerHostedService>();

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
