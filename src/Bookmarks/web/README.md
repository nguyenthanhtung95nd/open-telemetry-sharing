# Bookmarks web

React front end for [Bookmarks.Api](../services/Bookmarks.Api), built to show one thing:
**how a browser tells the backend which user action caused the work it is doing**, without
running an OpenTelemetry SDK in the browser.

Vite, React, TypeScript, Tailwind, TanStack Query.

---

## Run it

The API must be running first. See the [solution README](../README.md).

```bash
npm install
npm run dev
```

Open http://localhost:5173.

---

## The idea

There is **no OpenTelemetry package in this app**. No SDK, no exporter, no collector, no API
key in the browser. The entire mechanism is one HTTP header:

```
baggage: app.action=create-bookmark,app.screen=bookmarks
```

`baggage` is a W3C standard header. The API's OpenTelemetry setup reads it automatically, and
`Shared.Observability` copies the allow-listed keys onto **every span the request produces**,
including the SQL span. Verified output for one button press:

```
POST bookmarks          app.action: create-bookmark
 └─ Bookmarks.Create    app.action: create-bookmark
     └─ postgresql      app.action: create-bookmark
```

So in Elastic, or any backend, `app.action: "create-bookmark"` returns every piece of server
work one click caused.

### Why not the browser OpenTelemetry SDK

It is a real option, and the right one when you need page load timings, user interaction
spans or front end error tracking. It is also a much larger commitment: a sizeable bundle, a
telemetry endpoint reachable from the browser with CORS, and somewhere to put credentials
that is not the browser.

For most production systems the question is simply *"which user action caused this?"*, and a
baggage header answers that in about five lines.

### Trace id for support

Every response carries `X-Trace-Id`. The panel on the right shows it per call, click to copy.
That is the id to paste into a support ticket.

The front end does **not** generate `traceparent`. Doing so would declare a parent span that
is never exported, leaving an empty root in every trace. Letting the server own the trace and
report its id back keeps the data clean.

---

## Where to look

| File | Why |
|---|---|
| `src/api/client.ts` | The whole mechanism. One header out, one header in. |
| `src/telemetry/callLog.ts` | In-memory log of recent calls, feeds the panel. |
| `src/api/hooks.ts` | TanStack Query wrappers. Each names its `app.action`. |

Everything else is ordinary CRUD UI.

---

## Backend requirements

Two things in `Bookmarks.Api` make this work, both already configured:

```json
"Cors": { "AllowedOrigins": [ "http://localhost:5173" ] },
"Observability": { "Baggage": { "AllowedKeyPrefixes": [ "app." ] } }
```

CORS must allow the `baggage` request header and expose `X-Trace-Id`, otherwise the browser
strips one and cannot read the other. Baggage is opt-in by prefix because the header is
untrusted input from the caller.
