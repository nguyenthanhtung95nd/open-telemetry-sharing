using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Trace;
using Payment.Api.Data;
using Payment.Api.Diagnostics;
using Payment.Api.Logging;
using Payment.Api.Messaging;
using Payment.Api.Simulation;
using Shared.Messaging;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb")));

builder.Services
    .AddOptions<PaymentOptions>()
    .Bind(builder.Configuration.GetSection(PaymentOptions.SectionName))
    .Validate(o => o.FailureRate is >= 0 and <= 1, "Payment failure rate must be between 0 and 1.")
    .ValidateOnStart();

builder.Services.AddSingleton<IRandomProvider, RandomProvider>();
builder.Services.AddSingleton<IPaymentSimulator, PaymentSimulator>();

var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq must be configured.");
builder.Services.AddSingleton<IRabbitMqPublisher>(new RabbitMqPublisher(rabbitMqConnectionString));
builder.Services.AddSingleton<IPaymentEventsPublisher, PaymentEventsPublisher>();

builder.Services.AddSingleton<IEventHandler<StockReservedEvent>, StockReservedHandler>();
builder.Services.AddHostedService<StockReservedConsumerHostedService>();

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

app.Run();
