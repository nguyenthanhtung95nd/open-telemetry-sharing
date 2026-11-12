using Gateway.Api.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

var isRunningInContainer = builder.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
var ocelotConfigFile = isRunningInContainer ? "ocelot.Docker.json" : "ocelot.json";
builder.Configuration.AddJsonFile(ocelotConfigFile, optional: false, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration);

builder.AddSharedObservability();

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwaggerForOcelotUI(opt =>
{
    opt.PathToSwaggerGenerator = "/swagger/docs";
});

await app.UseOcelot();

app.Run();
