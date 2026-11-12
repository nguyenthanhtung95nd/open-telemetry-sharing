// Aspire practice only. Bookmarks.Api runs and deploys on its own without any of this.
// Aspire injects OTEL_EXPORTER_OTLP_ENDPOINT pointing at its dashboard, and environment
// variables win over appsettings, so telemetry appears there with no extra configuration.

var builder = DistributedApplication.CreateBuilder(args);

// No data volume: every run starts from an empty database.
var bookmarksDb = builder.AddPostgres("postgres")
    .AddDatabase("bookmarksdb");

builder.AddProject<Projects.Bookmarks_Api>("api")
    .WithReference(bookmarksDb)
    .WaitFor(bookmarksDb);

builder.Build().Run();
