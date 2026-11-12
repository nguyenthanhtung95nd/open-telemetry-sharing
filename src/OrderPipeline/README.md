# OrderPipeline

A small e-commerce order pipeline built as five .NET 8 services, used to demonstrate
distributed tracing with OpenTelemetry. Placing one order crosses a Next.js front end,
an Ocelot gateway, three backend services and a RabbitMQ choreography saga, and the
whole thing shows up in Jaeger as a single trace.

This README covers running and debugging the solution on your own machine. If you have
Docker and the .NET 8 SDK, [Quick start](#4-quick-start-with-docker-compose) is one
command and you need nothing else.

---

## Contents

1. [What this is](#1-what-this-is)
2. [Architecture at a glance](#2-architecture-at-a-glance)
3. [Prerequisites](#3-prerequisites)
4. [Quick start with Docker Compose](#4-quick-start-with-docker-compose)
5. [Debugging from your IDE](#5-debugging-from-your-ide)
6. [Ports, URLs and credentials](#6-ports-urls-and-credentials)
7. [Seeing the telemetry](#7-seeing-the-telemetry)
8. [Solution layout](#8-solution-layout)
9. [Running the tests](#9-running-the-tests)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. What this is

A storefront where a signed-in customer picks products, places an order, and watches it
move through stock reservation and payment in real time. An administrator can also
manage the product catalog and the user accounts.

The interesting part is not the shopping. It is that one click produces work in six
processes that never call each other directly, and that you can still see the whole
story as a single trace.

What the solution demonstrates:

- **Choreography saga.** No orchestrator. Each service reacts to events and publishes
  its own, including the compensating step that releases stock when payment fails.
- **Distributed tracing across a message broker.** Trace context is propagated through
  RabbitMQ, so a consumer span is a child of the HTTP request that caused it.
- **One telemetry pipeline.** Traces, metrics and logs all leave each service over OTLP
  to a single collector, which fans them out to Jaeger, Prometheus and Loki.
- **Database per service.** OrderPipeline, Inventory and Payment each own a database and
  never read each other's tables.
- **Authentication with Keycloak.** OpenID Connect with PKCE in the browser, JWT bearer
  validation in the services, and two realm roles.

---

## 2. Architecture at a glance

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/architecture-dark.svg">
  <img alt="Container diagram: the Next.js web app calls the Ocelot gateway, which fronts the OrderPipeline, Inventory and Identity APIs. Payment.Api is driven only by RabbitMQ events, and Keycloak issues the tokens." src="docs/architecture-light.svg">
</picture>

### The services

| Service | Owns | Talks to |
|---|---|---|
| **web** | Next.js storefront and admin console. Also a backend for frontend, so the browser never holds an access token | Gateway.Api, Keycloak |
| **Gateway.Api** | Ocelot reverse proxy. The single entry point for every backend call | all three APIs |
| **OrderPipeline.Api** | Orders and their status. `orderpipelinedb` | Inventory.Api, RabbitMQ |
| **Inventory.Api** | Product catalog and stock levels. `inventorydb` | RabbitMQ |
| **Payment.Api** | Payment attempts. `paymentdb`. No HTTP surface at all, it is driven entirely by events | RabbitMQ |
| **Identity.Api** | User administration, backed by the Keycloak admin API | Keycloak |

Three shared libraries carry the cross-cutting pieces: `Shared.Observability` wires up
OpenTelemetry, `Shared.Messaging` wraps RabbitMQ publish and consume, and
`Shared.Security` configures JWT bearer validation.

### The saga

All exchanges are durable fanout exchanges. A publisher never knows who is listening,
which is what makes this choreography rather than orchestration.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/order-saga-dark.svg">
  <img alt="Flowchart of the saga: an order is saved as Pending, Inventory.Api reserves stock, Payment.Api charges, and a declined charge releases the reserved stock automatically." src="docs/order-saga-light.svg">
</picture>

The arrows are labelled with the short form. These are the exchanges they correspond
to, which are the names you will see in the RabbitMQ management UI:

| Published by | Exchange | Consumed by |
|---|---|---|
| OrderPipeline.Api | `order.events` | Inventory.Api |
| Inventory.Api | `inventory.stock-reserved` | OrderPipeline.Api, Payment.Api |
| Inventory.Api | `inventory.stock-reservation-failed` | OrderPipeline.Api |
| Payment.Api | `payment.succeeded` | OrderPipeline.Api |
| Payment.Api | `payment.failed` | OrderPipeline.Api, Inventory.Api |

The compensating step is the dashed red line. Inventory.Api listens to `payment.failed`
and puts the reserved stock back, which is the only reason a declined payment does not
quietly lose inventory.

### Order status values

The API returns the status as a number. The web app maps it to a timeline.

| Value | Name | Meaning |
|---|---|---|
| 0 | `Pending` | Saved, waiting for Inventory.Api |
| 4 | `StockReserved` | Stock held, waiting for Payment.Api |
| 2 | `Paid` | Done, the happy path |
| 1 | `OutOfStock` | Inventory.Api refused the reservation |
| 3 | `PaymentFailed` | Charge declined, stock has been released |

Payment.Api declines 15 percent of attempts on purpose (`Payment:FailureRate`), so
roughly one order in seven exercises the compensating path. It also waits 2.5 seconds
before deciding (`Payment:ProcessingDelayMs` in `docker-compose.yml`), so the status
timeline is visible rather than instant.

---

## 3. Prerequisites

| Tool | Version | Needed for |
|---|---|---|
| Docker Desktop | any current version, with Compose v2 | every run mode |
| .NET SDK | 8.0 | building, testing, running services from an IDE |
| Node.js | 20 or newer | running the web app outside Docker |

For the quick start you need Docker only. The .NET SDK and Node are required for
[Debugging from your IDE](#5-debugging-from-your-ide).

Check what you have:

```bash
docker compose version
dotnet --version
node --version
```

You also need these host ports free: 3000, 3001, 4317, 4318, 5432, 5672, 8080,
8085, 8086, 8087, 8089, 9091, 15672 and 16687. See
[Troubleshooting](#10-troubleshooting) if port 5432 is already taken, which is the
common case.

---

## 4. Quick start with Docker Compose

Everything, including the database and its migrations, comes up with one command. There
is no database to create, no migration to apply and no `.env` file to write.

```bash
cd src/OrderPipeline
docker compose up -d --build
```

The first build takes a few minutes. When it finishes, open **http://localhost:3000**.

### What just happened

Compose starts the infrastructure first, then a one-shot `migrator` container that
applies every EF Core migration and exits. The three API services wait for it with
`condition: service_completed_successfully`, so none of them ever starts against an
empty schema.

Confirm the migrator did its job and exited cleanly:

```bash
docker compose ps -a --format "{{.Service}}: {{.Status}}" migrator
```

```
migrator: Exited (0) 2 minutes ago
```

`Exited (0)` is the expected state. The migrator is not supposed to keep running.

### Place your first order

1. Open http://localhost:3000
2. Click **Log in** and sign in as `admin@orderpipeline.local` with password `admin1234`
3. Add a product to your order and click **Place order**
4. Watch the timeline move through **Taking payment** to **Payment confirmed**

The storefront is seeded with three products: Keyboard at 100 in stock, Mouse at 200 and
Monitor at 50.

If you want to see the compensating path, place several orders. Roughly one in seven is
declined and ends at **Payment failed**, with the stock returned.

### Check it from the command line

The product catalog is the one endpoint that needs no token:

```bash
curl http://localhost:8080/products
```

```json
[{"id":1,"name":"Keyboard","price":49.99,"availableQuantity":100},
 {"id":2,"name":"Mouse","price":19.99,"availableQuantity":200},
 {"id":3,"name":"Monitor","price":199.99,"availableQuantity":50}]
```

### Everyday commands

```bash
# follow one service
docker compose logs -f orderpipeline.api

# restart one service after changing its code
docker compose up -d --build orderpipeline.api

# stop everything, keep the database
docker compose down

# stop everything and throw the database away, back to seed data
docker compose down -v
```

### Using a Postgres you already run

The committed defaults point at the bundled `postgres` service and need no edits. If you
would rather use your own server, copy `.env.example` to `.env` and set the three
connection strings.

The migrator creates each database if it is missing, because `dotnet ef database update`
does that before it applies migrations. That only works if the user in your connection
string holds `CREATEDB`, which is true of the bundled Postgres and often not true of a
managed one. If yours is restricted, create `orderpipelinedb`, `inventorydb` and
`paymentdb` on that server yourself first.

```bash
cp .env.example .env
```

The bundled Postgres still starts and still publishes 5432. If your own server already
holds that port, set `POSTGRES_PORT` in the same `.env` file, otherwise nothing starts
at all.

```bash
POSTGRES_PORT=5433
```

---

## 5. Debugging from your IDE

For breakpoints, run the infrastructure in Docker and the five services plus the web app
on your machine. Every service already has a `Development` profile pointing at
`localhost`, so there is nothing to configure.

### Step 1. Stop the containerised applications

The two modes collide on ports 3000, 8080, 8085, 8086 and 8087, so the containers have
to give those up first. Leave the infrastructure running.

```bash
cd src/OrderPipeline
docker compose stop gateway.api orderpipeline.api inventory.api payment.api identity.api web
```

```bash
docker compose ps --format "{{.Service}}: {{.State}}"
```

You should be left with `postgres`, `rabbitmq`, `keycloak`, `otel-collector`, `jaeger`,
`prometheus`, `loki` and `grafana`.

### Step 2. Build once

Five services share three projects. Starting them all at once makes five builds fight
over the same output files, so build the solution first and then run without rebuilding.

```bash
dotnet build
```

### Step 3. Start the services

In your IDE, start each project on its **default profile**. From a terminal the
equivalent is one of these per service, each in its own window:

```bash
dotnet run --project services/Identity.Api --no-build
dotnet run --project services/Inventory.Api --no-build
dotnet run --project services/Payment.Api --no-build
dotnet run --project services/OrderPipeline.Api --no-build
dotnet run --project services/Gateway.Api --no-build
```

Start Gateway.Api last. Each service prints the port it took, and those ports are the
ones `services/Gateway.Api/ocelot.json` routes to:

```
Now listening on: http://localhost:8087   (Identity.Api)
Now listening on: http://localhost:8086   (Inventory.Api)
Now listening on: http://localhost:8084   (Payment.Api)
Now listening on: http://localhost:8085   (OrderPipeline.Api)
Now listening on: http://localhost:8080   (Gateway.Api)
```

Check that the gateway reaches Inventory.Api and the database:

```bash
curl http://localhost:8080/products
```

### Step 4. Start the web app

```bash
cd web
cp .env.local.example .env.local
npm install
npm run dev
```

Open http://localhost:3000 and place an order. Breakpoints in any of the five services
will now hit.

### Going back to Docker

Stop the dev server and the five services first, because they hold the ports the
containers need. Then:

```bash
docker compose up -d
```

### How configuration resolves

Each service reads `appsettings.Development.json`, which points at `localhost`. In
Docker, Compose sets the same keys as environment variables, and an environment variable
always wins over a file. That is why one set of files serves both run modes.

| Setting | IDE value | Docker value |
|---|---|---|
| `ConnectionStrings:*Db` | `Host=localhost;Port=5432` | `Host=postgres;Port=5432` |
| `ConnectionStrings:RabbitMq` | `amqp://guest:guest@localhost:5672/` | `amqp://guest:guest@rabbitmq:5672/` |
| `Keycloak:Authority` | `http://localhost:8089/realms/orderpipeline` | `http://keycloak:8080/realms/orderpipeline` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://localhost:4317` | `http://otel-collector:4317` |

The gateway picks its routing file the same way. Outside a container it reads
`ocelot.json`, and inside one it reads `ocelot.Docker.json`, selected by the
`DOTNET_RUNNING_IN_CONTAINER` variable.

---

## 6. Ports, URLs and credentials

### Application

| Component | URL | Notes |
|---|---|---|
| Storefront | http://localhost:3000 | start here |
| Admin console | http://localhost:3000/admin | requires the `admin` role |
| Gateway.Api | http://localhost:8080 | the only backend entry point |
| OrderPipeline.Api | http://localhost:8085 | Swagger at `/swagger` |
| Inventory.Api | http://localhost:8086 | Swagger at `/swagger` |
| Identity.Api | http://localhost:8087 | Swagger at `/swagger` |
| Payment.Api | http://localhost:8084 | IDE runs only, no port is published in Docker |

### Infrastructure

| Component | URL | Credentials |
|---|---|---|
| Keycloak | http://localhost:8089 | `admin` / `admin` |
| RabbitMQ management | http://localhost:15672 | `guest` / `guest` |
| Jaeger | http://localhost:16687 | none |
| Grafana | http://localhost:3001 | anonymous, already an Admin |
| Prometheus | http://localhost:9091 | none |
| Loki | http://localhost:3101 | queried through Grafana |
| Postgres | `localhost:5432` | `app` / `app` |
| OTLP gRPC and HTTP | `localhost:4317`, `localhost:4318` | used by the services |

### Application accounts

One account is seeded in `deploy/keycloak-realm.json`:

| Account | Password | Roles |
|---|---|---|
| `admin@orderpipeline.local` | `admin1234` | `admin`, `customer` |

Create more customers at http://localhost:3000/register, which is open to anonymous
callers, or from the admin console at http://localhost:3000/admin/users.

Every one of these values is a local throwaway. None of it belongs anywhere but a
developer machine.

### Gateway routes

| Public route | Goes to |
|---|---|
| `GET POST /orders`, `/orders/{id}` | OrderPipeline.Api `/api/orders` |
| `GET POST PUT DELETE /products`, `/products/{id}` | Inventory.Api `/api/products` |
| `GET /inventory/{productId}/available` | Inventory.Api |
| `POST /identity/register` | Identity.Api, anonymous |
| `/identity/users`, `/identity/users/{id}` | Identity.Api, `admin` only |

---

## 7. Seeing the telemetry

Every service exports traces, metrics and logs over OTLP to the collector, which routes
traces to Jaeger, metrics to Prometheus and logs to Loki.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/telemetry-pipeline-dark.svg">
  <img alt="The web app and the five .NET services export traces, metrics and logs over OTLP to one OpenTelemetry Collector, which fans them out to Jaeger, Prometheus and Loki." src="docs/telemetry-pipeline-light.svg">
</picture>

### Traces in Jaeger

1. Place an order at http://localhost:3000
2. Open http://localhost:16687
3. Pick service **OrderPipeline.Api** and click **Find Traces**
4. Open the longest trace

A full checkout is roughly 30 to 35 spans across six services: `Web.Browser`, `Web.BFF`,
`Gateway.Api`, `OrderPipeline.Api`, `Inventory.Api` and `Payment.Api`. The spans below
the publish are RabbitMQ consumers in other processes, linked by trace context carried
in the message headers.

**If the trace list looks empty, that is probably sampling, not a bug.** The collector
tail samples at 50 percent, so about half of all successful checkouts are dropped.
Traces containing an error are always kept. Place a few orders before concluding
anything is broken, or lower the rate in `deploy/otel-config.yaml`:

```yaml
      - name: sample-the-rest
        type: probabilistic
        probabilistic:
          sampling_percentage: 100
```

Then restart the collector:

```bash
docker compose up -d --force-recreate otel-collector
```

### Metrics in Prometheus and Grafana

Prometheus scrapes the collector at http://localhost:9091. Grafana at
http://localhost:3001 is already provisioned with Prometheus, Loki and Jaeger as data
sources, and anonymous access is on, so no login is needed.

Try `http_server_request_duration_seconds_count` in the Prometheus expression browser
after placing a few orders.

### Logs in Loki

Loki is queried through Grafana rather than directly. Open **Explore**, choose the
**Loki** data source, and start with a service selector. The label is `job`, and its
value is the OpenTelemetry namespace followed by the service name:

```logql
{job="OpenTelemetrySharing.Course/OrderPipeline.Api"}
```

The available labels are `job`, `level`, `exporter` and `instance`. To follow one
service at one severity:

```logql
{job="OpenTelemetrySharing.Course/Payment.Api", level="WARN"}
```

Every line carries the trace id, so you can move from a slow span in Jaeger to the exact
log lines it produced:

```json
{
  "body": "Order {OrderId} marked Paid",
  "traceid": "cae12822cba59dfc18fcf23ac8dfd604",
  "spanid": "2dd026d666f1e7a8"
}
```

### Raw telemetry, without any UI

Every pipeline also carries the collector's `debug` exporter, which prints each batch of
spans, metrics and log records as it arrives. That is the fastest way to tell "the
service never exported anything" apart from "the backend is not showing it":

```bash
docker compose logs -f otel-collector
```

The collector deliberately writes nothing to disk. A file exporter costs disk I/O on the
hot path and grows without bound, and the `debug` exporter answers the same question.

---

## 8. Solution layout

```
src/OrderPipeline/
├── OrderPipeline.sln
├── docker-compose.yml           every service and all the infrastructure
├── Directory.Packages.props     one pinned version per package, for every project
├── .env.example                 optional, for your own Postgres
│
├── services/                    what runs, one process per folder
│   ├── Gateway.Api/             Ocelot, one route table per run mode
│   ├── OrderPipeline.Api/       orders, the saga's status owner
│   ├── Inventory.Api/           catalog, stock, the compensating release
│   ├── Payment.Api/             event-driven only, no controllers
│   └── Identity.Api/            users, over the Keycloak admin API
│
├── shared/                      libraries the services depend on
│   ├── Shared.Observability/    OpenTelemetry setup for all five services
│   ├── Shared.Messaging/        RabbitMQ publish, consume, event contracts
│   └── Shared.Security/         JWT bearer validation
│
├── tests/                       one xUnit project per service and per library
│
├── deploy/                      config the infrastructure containers mount
│   ├── Migrator.Dockerfile      one-shot container, creates and migrates the databases
│   ├── otel-config.yaml         collector pipelines and tail sampling
│   ├── prometheus.yml           scrape config
│   ├── grafana-datasources.yml  Prometheus, Loki and Jaeger, provisioned
│   ├── keycloak-realm.json      realm, clients, roles, seeded admin
│   └── rabbitmq-policy.json     dead-letter policy for every saga queue
│
├── docs/                        the diagrams this README embeds
└── web/                         Next.js 16 storefront, admin and BFF
```

The four folders divide the solution by what each thing is for. `services/` holds what
runs, and each of those folders carries its own `Dockerfile`. `shared/` holds the three
libraries the services depend on, and which depend on nothing in the solution
themselves. `tests/` mirrors both, one xUnit project per service and per library.
`deploy/` holds configuration that only the infrastructure containers read.

`docker-compose.yml` deliberately stays at the root rather than moving into `deploy/`,
because Compose takes its project name and its `.env` from whichever directory holds it.
Moving it would rename every container and orphan the `postgres-data` volume.

`web/` is not part of `OrderPipeline.sln`, since Next.js has no project type a `.sln`
can carry. Open it directly, or see [web/README.md](./web/README.md).

Each API project follows the same shape: `Controllers/` for HTTP, `Services/` for
business logic behind an interface, `Data/` for the DbContext and `Migrations/`, and
`Messaging/` for the hosted services that consume events.

The diagrams in `docs/` ship as a light and a dark SVG each, with the matching `.html`
kept beside them as the editable source.

---

## 9. Running the tests

```bash
cd src/OrderPipeline
dotnet test
```

```
Passed!  - Failed: 0, Passed: 18, Skipped: 0, Total: 18 - Identity.Api.Tests.dll
Passed!  - Failed: 0, Passed:  9, Skipped: 0, Total:  9 - Shared.Messaging.Tests.dll
Passed!  - Failed: 0, Passed: 43, Skipped: 0, Total: 43 - Inventory.Api.Tests.dll
Passed!  - Failed: 0, Passed: 28, Skipped: 0, Total: 28 - OrderPipeline.Api.Tests.dll
Passed!  - Failed: 0, Passed: 56, Skipped: 0, Total: 56 - Shared.Observability.Tests.dll
Passed!  - Failed: 0, Passed: 19, Skipped: 0, Total: 19 - Payment.Api.Tests.dll
```

173 tests across six projects, all unit tests with xUnit and NSubstitute. They need
nothing running, so Docker can be off.

One project at a time:

```bash
dotnet test tests/Inventory.Api.Tests
```

If services are already running from your IDE, they hold the build output. Use
`dotnet test --no-build` to avoid the file lock.

### Adding a migration

`dotnet ef` does not ship with the SDK, so install it once. Pin it to 8.0.11, which is
the EF Core version in `Directory.Packages.props` and the one
`deploy/Migrator.Dockerfile` installs. A newer tool emits migrations this runtime
cannot read.

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef migrations add YourMigrationName --project services/Inventory.Api
```

The migrator container applies it on the next `docker compose up`.

---

## 10. Troubleshooting

### Nothing starts, and the error mentions port 5432

Your machine already runs a Postgres. The bundled one publishes the same port, so
Compose cannot bind it.

```bash
cp .env.example .env
```

Set `POSTGRES_PORT=5433` in `.env` and start again. Your own Postgres keeps 5432 and
keeps its data.

### An edit to deploy/keycloak-realm.json seems to do nothing

Keycloak imports a realm only when that realm does not already exist, and it keeps the
realm in a file inside the container. A container that survives `docker compose up`
therefore serves the old realm forever, which looks like your edit was ignored.

Recreate the container, do not restart it:

```bash
docker compose up -d --force-recreate keycloak
```

Every account created at runtime is lost when you do this, which is correct for a realm
whose source of truth is the file.

### Login fails with the right password

If the Keycloak log contains `Table "USER_ENTITY" not found (this database is empty)`,
Keycloak is running with its database in memory. An in-memory H2 database only lives as
long as a connection to it is open, so a few minutes after boot the connection pool goes
idle and every user disappears. The login page reports this as a wrong password.

Make sure `docker-compose.yml` sets no `KC_DB` for the keycloak service. The default
file-backed storage is the stable one.

```bash
docker compose logs keycloak | grep USER_ENTITY
```

### Orders stay at "Processing" forever

The saga is not running. Almost always this means the RabbitMQ consumers died at startup
because the services got there before the broker's AMQP listener was ready. Orders are
still accepted, so nothing looks wrong until you notice the status never moves.

Check for consumers:

```bash
docker compose exec rabbitmq rabbitmqctl list_queues name consumers
```

Every queue should show `1`. If they show `0`, restart the services:

```bash
docker compose up -d --force-recreate orderpipeline.api inventory.api payment.api
```

Note that `rabbitmq-diagnostics ping` is not a useful check here. It answers as soon as
the Erlang node is up, which is earlier than the AMQP listener. The compose health check
uses `check_port_connectivity` for that reason.

### Jaeger shows no traces

Expected about half the time. The collector tail samples at 50 percent. See
[Traces in Jaeger](#traces-in-jaeger).

### The migrator container exits 1

Read its log, it is short:

```bash
docker compose logs migrator
```

A connection error means Postgres was not reachable, which you can confirm with
`docker compose ps postgres` showing `healthy`. `dotnet-ef does not exist` means the
tool install layer did not run, so rebuild with
`docker compose build --no-cache migrator`.

### A service in my IDE starts on the wrong port

The default profile of each project is the first one in
`Properties/launchSettings.json`. Picking a different profile can give you a different
port, and `ocelot.json` routes to fixed ports. Check the "Now listening on" line against
[the port table](#application).

### Port already in use when returning to Docker

The IDE-run services and the containers collide on 3000, 8080, 8085, 8086 and 8087. Stop
the IDE processes and the Next.js dev server before `docker compose up -d`.

### Starting fresh

This throws away the database volume and every container, then rebuilds:

```bash
docker compose down -v
docker compose up -d --build
```

Stock returns to the seeded 100, 200 and 50, and all orders are gone.

---

## Related

- [web/README.md](./web/README.md) for the Next.js app on its own
- `services/Gateway.Api/ocelot.json` for the full route table
- `deploy/otel-config.yaml` for the collector pipelines
