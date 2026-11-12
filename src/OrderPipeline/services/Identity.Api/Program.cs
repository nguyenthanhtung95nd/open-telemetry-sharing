using Identity.Api.Keycloak;
using Identity.Api.Diagnostics;
using Identity.Api.Logging;
using Identity.Api.Services;
using Shared.Observability;
using Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));

var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"]
    ?? throw new InvalidOperationException("Keycloak:BaseUrl must be configured.");
builder.Services.AddHttpClient<KeycloakAdminHttp>(client =>
    client.BaseAddress = new Uri(keycloakBaseUrl));

builder.Services.AddScoped<IKeycloakUserClient, KeycloakUserClient>();
builder.Services.AddScoped<IKeycloakRoleClient, KeycloakRoleClient>();

builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();

builder.AddKeycloakAuthentication();

builder.AddSharedObservability(observability =>
{
    observability.ActivitySources.Add(ApplicationDiagnostics.ActivitySource.Name);

    foreach (var eventId in LogEventIds.Audited) observability.Audit.EventIds.Add(eventId);
});

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
