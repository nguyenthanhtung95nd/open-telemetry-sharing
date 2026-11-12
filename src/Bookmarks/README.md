# Bookmarks

A REST API and React front end for saving links, built as a working reference for
OpenTelemetry in .NET. The domain is deliberately plain so that nothing competes with the
telemetry for your attention.

Setup takes about ten minutes and ends with a real trace you can read, a browser action you
can follow all the way into a SQL statement, and a backend you can swap by editing three
settings.

---

## Contents

1. [What this shows](#1-what-this-shows)
2. [Prerequisites](#2-prerequisites)
3. [Run it](#3-run-it)
4. [Verify it works](#4-verify-it-works)
5. [How observability works](#5-how-observability-works)
6. [Connecting the front end to the back end](#6-connecting-the-front-end-to-the-back-end)
7. [Sending telemetry to Elastic Cloud](#7-sending-telemetry-to-elastic-cloud)
8. [The API](#8-the-api)
9. [Where the telemetry is configured](#9-where-the-telemetry-is-configured)
10. [The Aspire AppHost, and when you need it](#10-the-aspire-apphost-and-when-you-need-it)
11. [Do you need an OpenTelemetry Collector](#11-do-you-need-an-opentelemetry-collector)
12. [Solution layout](#12-solution-layout)
13. [Running the tests](#13-running-the-tests)
14. [Gaps found in Shared.Observability](#14-gaps-found-in-sharedobservability)
15. [Troubleshooting](#15-troubleshooting)

---

## 1. What this shows

**Automatic instrumentation is nearly free.** Incoming HTTP requests, database queries and
process runtime counters are reported without a single line written at any call site.

**Manual instrumentation fills the gaps.** The service layer starts a span per business
operation and records two metrics nobody could derive from HTTP alone.

**A browser can join the trace without an SDK.** The front end sends one standard header and
the back end attributes every span, down to the SQL, to the button that caused it.

**The backend is a configuration value.** Pointing this at Elastic Cloud, or anything else
that speaks OTLP, means editing three settings. No code change, no rebuild.

**Telemetry setup is one call.** `Shared.Observability` owns traces, metrics, logs, sampling
and exporters. The service registers it once and names what it emits. Nothing else in the
codebase touches the OpenTelemetry SDK.

---

## 2. Prerequisites

| Tool | Version | Check with |
|---|---|---|
| .NET SDK | 10.0 or later | `dotnet --version` |
| Node.js | 20 or later | `node --version` |
| Docker | any recent version, **running** | `docker ps` |

`global.json` pins the .NET SDK band, so `dotnet` picks the right one automatically.

You do **not** need the Aspire CLI, an OpenTelemetry Collector, or any cloud account to
complete this README.

---

## 3. Run it

Three processes: a database, the API, and the front end. Use three terminals.

### Terminal 1: PostgreSQL

```bash
docker run -d --name bookmarks-db -p 5432:5432 \
  -e POSTGRES_DB=bookmarks \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  postgres:18.3
```

Confirm it is up:

```bash
docker ps --filter name=bookmarks-db
```

You should see one row with status `Up`. The connection string matching these values is
already in `services/Bookmarks.Api/appsettings.Development.json`, so there is nothing to edit.

### Terminal 2: the API

```bash
cd src/Bookmarks/services/Bookmarks.Api
dotnet run
```

The first run applies database migrations automatically. Watch for these lines:

```
Applying migration '20261105055505_InitialCreate'.
Applying migration '20261105065643_AddTags'.
Now listening on: http://localhost:5280
Hosting environment: Development
```

`Hosting environment: Development` matters. In any other environment migrations do not run
and the OpenAPI document is not served.

You will also see large blocks of text beginning with `Activity.TraceId` and
`LogRecord.Timestamp`. That is not noise, it is the console exporter printing your telemetry.
[Section 5](#5-how-observability-works) explains how to read it.

### Terminal 3: the front end

```bash
cd src/Bookmarks/web
npm install
npm run dev
```

```
VITE ready in 400 ms
Local: http://localhost:5173/
```

Open http://localhost:5173.

### Ports

| Service | URL | Set in |
|---|---|---|
| Front end | http://localhost:5173 | `web/vite.config.ts` |
| API | http://localhost:5280 | `services/Bookmarks.Api/Properties/launchSettings.json` |
| OpenAPI document | http://localhost:5280/openapi/v1.json | Development only |
| Health check | http://localhost:5280/health | every environment |
| PostgreSQL | localhost:5432 | the `docker run` above |

If you change the front end port, add the new origin to `Cors:AllowedOrigins` in
`services/Bookmarks.Api/appsettings.Development.json`, or the browser will block every call.

### Stopping

`Ctrl+C` in terminals 2 and 3, then:

```bash
docker rm -f bookmarks-db
```

The container has no volume, so stopping it discards the data. That is deliberate: every run
starts from an empty database, so this README behaves the same on a machine that has run it
before.

---

## 4. Verify it works

### Without the front end

```bash
curl http://localhost:5280/bookmarks
```

```json
{"items":[],"page":1,"pageSize":10,"totalCount":0,"totalPages":0,"hasPreviousPage":false,"hasNextPage":false}
```

An empty collection, not an error. For everything else open
`services/Bookmarks.Api/Bookmarks.Api.http` in Visual Studio, VS Code or Rider and send the
requests from there. It covers the full lifecycle and every failure path, each labelled with
the status code it should return.

### With the front end

1. Open http://localhost:5173
2. Fill in **Title** and **URL**, press **Add bookmark**
3. The bookmark appears in the list below

Look at the **API calls** panel on the right. Each row shows the action name, the HTTP status,
and the trace id returned by the server:

```
create-bookmark                    201
POST /bookmarks
857301c8d5c8e450d480d86584bfa0ac   216 ms
```

Click a trace id to copy it. That is the identifier you would paste into a support ticket or
search for in a telemetry backend.

---

## 5. How observability works

### Start with your terminal, not a dashboard

`Observability:Export:Console` is on in Development, so every span, metric and log is printed
as it is produced. Read one before opening any UI. It is the clearest way to understand what
a span actually is.

Call `GET /bookmarks` and look for this in terminal 2:

```
Activity.TraceId:            c071e36adec84e2b2c452bbc0dc30ea7
Activity.SpanId:             84113f521a72f0b5
Activity.DisplayName:        GET bookmarks
Activity.Kind:               Server
```

A span is a named, timed operation with an identity. `TraceId` is shared by everything in one
request. `SpanId` identifies this step. `ParentSpanId`, on the spans below it, is what builds
the tree.

### The three spans of one request

```
GET bookmarks              Server     <- automatic, ASP.NET Core instrumentation
  └─ Bookmarks.List        Internal   <- manual, written in BookmarkService
      └─ postgresql        Client     <- automatic, the Npgsql driver
```

Only the middle one was written by hand. It carries `bookmark.count`, a value no generic
instrumentation could know.

The database span is worth noticing. Nobody instrumented the data layer. The Npgsql driver
publishes its own activity source, and the service subscribes to it by name:

```csharp
observability.ActivitySources.Add(ApplicationDiagnostics.NpgsqlActivitySourceName); // "Npgsql"
```

Any library that publishes an activity source switches on the same way, from configuration.

### Metrics

Two come from the `Bookmarks.Api` meter:

| Metric | Type | Why it exists |
|---|---|---|
| `bookmarks.created` | Counter | HTTP metrics cannot answer this. A POST that fails validation is still a POST. |
| `bookmarks.operation.duration` | Histogram, tagged by `operation` | Excludes model binding, serialization and middleware, so comparing it with HTTP duration shows how much of a slow request was your own code. |

Both are recorded by `ServiceOperation`, which starts the span and the timer together so the
two signals cannot drift apart.

### Logs

Log records carry the `TraceId` and `SpanId` of the request that produced them. That is what
lets you jump from a log line to the trace it belongs to.

Adding OpenTelemetry does **not** replace console logging. The standard console logger keeps
running, so a platform that collects stdout, CloudWatch for example, keeps working exactly as
before. OpenTelemetry adds a second path alongside it.

> The console exporter duplicates every signal and writes on the hot path. It belongs in a
> tutorial, not in production. Turn it off with `Observability:Export:Console`.

---

## 6. Connecting the front end to the back end

The usual production question is not *"can I see spans"*, it is **"which user action caused
this work?"**. This project answers that with one header and no browser SDK.

### What the front end sends

`web/src/api/client.ts` adds this to every request:

```
baggage: app.action=create-bookmark,app.screen=bookmarks
```

`baggage` is a W3C standard header. There is **no OpenTelemetry package in the front end**,
no exporter, no collector, and no credentials in the browser.

### What the back end does with it

Nothing, in the sense that nobody wrote code for it. The OpenTelemetry SDK reads the header,
and `Shared.Observability`'s `BaggageTagProcessor` copies allow-listed keys onto every span.
Enabling it is one setting:

```json
"Observability": {
  "Baggage": { "AllowedKeyPrefixes": [ "app." ] }
}
```

Keys are opt-in by prefix because `baggage` is untrusted input from the caller.

### The result

One button press, taken from the console exporter:

```
POST bookmarks          app.action: create-bookmark
 └─ Bookmarks.Create    app.action: create-bookmark
     └─ postgresql      app.action: create-bookmark
```

Searching `app.action: "create-bookmark"` in any backend returns every piece of server work
that click caused, including the SQL.

### The trace id going back

Every response carries `X-Trace-Id`, added by `TraceIdResponseMiddleware`. The front end reads
it and shows it in the panel.

The front end deliberately does **not** generate `traceparent` itself. Doing so would declare
a parent span that is never exported, leaving an empty root in every trace. Letting the server
own the trace and report its id back keeps the data clean.

### Two CORS settings this depends on

```json
"Cors": { "AllowedOrigins": [ "http://localhost:5173" ] }
```

The policy must allow `baggage` as a request header, or the browser's preflight drops it, and
must expose `X-Trace-Id`, or JavaScript cannot read it. Both are configured in `Program.cs`.

---

## 7. Sending telemetry to Elastic Cloud

Three lines of configuration, no code change.

Get the managed OTLP endpoint and an API key from the Elastic Cloud console. The key needs at
least the `event:write` privilege for the `apm` application. The key copied from Kibana does
**not** include the `ApiKey` scheme, so you add it yourself.

In `services/Bookmarks.Api/appsettings.Development.json`:

```json
"OTEL_EXPORTER_OTLP_ENDPOINT": "https://YOUR_ELASTIC_ENDPOINT:443",
"OTEL_EXPORTER_OTLP_PROTOCOL": "grpc",
"OTEL_EXPORTER_OTLP_HEADERS": "Authorization=ApiKey YOUR_API_KEY"
```

Restart the API. That is the whole change.

**Do not commit the key.** Leave the value empty in the file and override it locally:

```bash
cd src/Bookmarks/services/Bookmarks.Api
dotnet user-secrets set "OTEL_EXPORTER_OTLP_HEADERS" "Authorization=ApiKey YOUR_API_KEY"
```

User secrets load after `appsettings.json`, so they win.

### Why this works

These are the OpenTelemetry specification's own environment variable names. The .NET SDK reads
them from `IConfiguration`, not only from the process environment, and in ASP.NET Core
`IConfiguration` includes `appsettings.json` and user secrets. So the standard names work as
ordinary settings.

`Shared.Observability` treats an empty endpoint as "no backend configured" and switches OTLP
export off, which is why a fresh clone runs with no connection errors.

### Any other backend

Nothing here is Elastic-specific. Elastic is simply an OTLP endpoint with an `Authorization`
header. Any backend that speaks OTLP drops into the same three settings.

---

## 8. The API

| Method | Route | Notes |
|---|---|---|
| `GET` | `/bookmarks` | Paged. Supports `q`, `kind`, `status`, `sort`, `page`, `pageSize`. |
| `POST` | `/bookmarks` | 201 with the created bookmark. |
| `GET` | `/bookmarks/{id}` | 404 when it does not exist. |
| `PUT` | `/bookmarks/{id}` | Replaces every field. |
| `DELETE` | `/bookmarks/{id}` | 204. |
| `PUT` | `/bookmarks/{id}/tags` | Replaces the whole tag set. All or nothing. |
| `GET` | `/tags` | Ordered by name. |
| `POST` | `/tags` | 409 when the name is taken, ignoring case. |
| `GET` `PUT` `DELETE` | `/tags/{id}` | |
| `GET` | `/health` | Mapped in every environment, for a load balancer or scheduler. |

Sorting accepts one field and an optional direction, for example `?sort=title desc`. Only
`title`, `createdAtUtc`, `status` and `kind` can be sorted on. Anything else is a 400 that
names the field you asked for. `pageSize` is capped at 100.

### Two bugs this project does not inherit

The implementation this was adapted from builds its sort clause by interpolating the caller's
text into a dynamic LINQ expression, then resolves the field with a lookup that throws when it
does not match, so an unknown sort field surfaced as a 500. It also passed `pageSize` straight
through with no ceiling, letting one request read the whole table.

Both are fixed here, and both have tests. See `BookmarkSorting` and
`BookmarksQueryParametersValidator`.

---

## 9. Where the telemetry is configured

Read these in order.

| File | What it does |
|---|---|
| `services/Bookmarks.Api/Program.cs` | The single `AddSharedObservability` call and the three names it registers |
| `services/Bookmarks.Api/Diagnostics/ApplicationDiagnostics.cs` | The only place an activity source or meter is constructed |
| `services/Bookmarks.Api/Services/BookmarkService.cs` | The hand-written spans and metric recording |
| `services/Bookmarks.Api/appsettings*.json` | Service identity, baggage policy, and where telemetry is sent |
| `web/src/api/client.ts` | The front end side: one header out, one header in |

---

## 10. The Aspire AppHost, and when you need it

`apphost/Bookmarks.AppHost` exists to **practise with .NET Aspire**. It is not required to
run, debug or deploy anything, and section 3 does not use it.

```bash
cd src/Bookmarks
dotnet run --project apphost/Bookmarks.AppHost
```

That starts PostgreSQL and the API together and gives you the Aspire dashboard, a local UI for
traces, metrics and logs with no cloud account.

Aspire injects `OTEL_EXPORTER_OTLP_ENDPOINT` as an environment variable, and environment
variables load after `appsettings.json`, so the AppHost overrides whatever the file says. Both
paths work without editing configuration.

It keeps a `Properties/launchSettings.json` so it runs in Development. Aspire loads user
secrets only in Development, and stores generated values such as the database password there,
so deleting that file causes quiet, confusing failures.

The AppHost does not start the front end. Run it separately as in section 3.

---

## 11. Do you need an OpenTelemetry Collector

Usually not, and this project does not ship one.

A collector is a separate process. It earns its place when:

| Situation | Collector? |
|---|---|
| One service, one backend that speaks OTLP | **No** |
| A backend that cannot accept OTLP, such as Prometheus or Loki | Yes, as a translator |
| Fanning the same telemetry out to two or more backends | Yes |
| Changing sampling or scrubbing PII without redeploying services | Yes |
| One egress point and one credential for many services | Yes |

This project sends OTLP to a backend that accepts OTLP, so a collector here would receive
OTLP and forward the same OTLP unchanged. Add one when a row in that table becomes true for
you, and not before.

---

## 12. Solution layout

```
src/Bookmarks/
  services/Bookmarks.Api/             The service. Runs on its own.
  web/                                React front end. Vite, Tailwind, TanStack Query.
  shared/Shared.Observability/        Shared telemetry library, net8.0.
  tests/Bookmarks.Api.Tests/          Business rules.
  tests/Shared.Observability.Tests/   Tests for that library.
  apphost/Bookmarks.AppHost/          Aspire practice only. Not needed to run or deploy.
```

`Shared.Observability` is a **vendored copy**, not a package or a project reference. Treat it
as read only. Improvements belong in its source of truth so every consumer gets them, not in
this copy where they would silently diverge.

---

## 13. Running the tests

```bash
cd src/Bookmarks
dotnet test
```

90 tests. 56 cover `Shared.Observability`, 34 cover this service's business rules.

The library targets `net8.0` while its test project targets `net10.0`, so the suite exercises
it under the same runtime the service uses.

The front end has no tests. It is a demonstration surface, and its only interesting logic is
the one header in `client.ts`.

---

## 14. Gaps found in Shared.Observability

Building this surfaced one real limitation. It was not fixed here on purpose, because the
library is a vendored copy and a local patch would diverge from its source of truth.

**No option for database instrumentation.** `ObservabilityInstrumentationOptions` covers
AspNetCore, HttpClient and Runtime only. Database spans work here because the Npgsql driver
publishes its own activity source and naming it in configuration is enough. That works, but a
consumer has to know to do it.

An earlier version of this document also claimed the library offered no way to set OTLP
headers, so authenticating directly to a backend was impossible without a collector. **That was
wrong.** The library does not expose headers through its own `Observability:Export` section,
but the OpenTelemetry SDK reads `OTEL_EXPORTER_OTLP_HEADERS` from `IConfiguration`, so ordinary
configuration works. Section 7 relies on this, and it is verified.

---

## 15. Troubleshooting

**`dotnet run` fails with a connection error about PostgreSQL.**
The container is not running, or not on `localhost:5432`. Run
`docker ps --filter name=bookmarks-db`. If it is missing, repeat terminal 1 in section 3.

**The API starts but says `Hosting environment: Production`.**
`Properties/launchSettings.json` is missing or was not picked up. In Production, migrations do
not run and OpenAPI is not served, so the first request fails with a missing table.

**The front end loads but every call fails, and the browser console mentions CORS.**
The front end origin is not in `Cors:AllowedOrigins`. The default allows
`http://localhost:5173` only. If Vite chose a different port because 5173 was taken, either
free that port or add the new origin and restart the API.

**Calls succeed but the trace id column says "no trace id".**
`X-Trace-Id` is not reaching the browser. A cross-origin response header is invisible to
JavaScript unless the server exposes it, which `Program.cs` does with `WithExposedHeaders`.

**Spans appear but carry no `app.action`.**
`Observability:Baggage:AllowedKeyPrefixes` is empty or does not include `app.`. With no
prefixes the processor copies nothing, which is the safe default.

**No telemetry anywhere.**
`Observability:Export:Console` turns on terminal output. If console works but your backend
sees nothing, the problem is the endpoint or the credentials, not the instrumentation.

**Nothing reaches Elastic Cloud.**
A 401 means the API key is wrong or lacks `event:write` on the `apm` application. The key from
Kibana does not include the `ApiKey` scheme, and the header value must be
`Authorization=ApiKey YOUR_API_KEY`.

Note that the .NET SDK reports exporter failures on an EventSource rather than through
`ILogger`, so **nothing appears in the application log** when delivery fails. The application
keeps serving normally either way: telemetry export never blocks a request. Enable
OpenTelemetry self-diagnostics if you need to see those errors.

**`npm install` fails or the dev server will not start.**
Node 20 or later is required. Delete `web/node_modules` and `web/package-lock.json`, then run
`npm install` again.

**The AppHost will not start.**
It needs Docker running. If it starts but ignores user secrets, confirm
`apphost/Bookmarks.AppHost/Properties/launchSettings.json` still exists, since secrets load
only in Development.
