import { clearCalls, useCallLog } from "../telemetry/callLog";

function statusClass(status: number): string {
  if (status >= 500) return "bg-red-100 text-red-800";
  if (status >= 400) return "bg-amber-100 text-amber-900";
  return "bg-emerald-100 text-emerald-800";
}

export function CallLogPanel() {
  const calls = useCallLog();

  return (
    <section aria-labelledby="calls-heading" className="flex h-full flex-col">
      <header className="flex items-baseline justify-between gap-4">
        <h2 id="calls-heading" className="text-sm font-semibold text-slate-700">
          API calls
        </h2>
        <button
          type="button"
          onClick={clearCalls}
          className="rounded px-2 py-1 text-xs text-slate-500 hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
        >
          Clear
        </button>
      </header>

      <p className="mt-1 text-xs leading-relaxed text-slate-500">
        Each call sends <code className="font-mono">app.action</code> as baggage. The server
        copies it onto every span, down to the SQL. Search that value in your telemetry
        backend, or search the trace id below.
      </p>

      {calls.length === 0 ? (
        <p className="mt-6 text-sm text-slate-400">Nothing yet. Do something on the left.</p>
      ) : (
        <ol className="mt-3 flex-1 space-y-2 overflow-y-auto pr-1">
          {calls.map((call) => (
            <li key={call.id} className="rounded border border-slate-200 bg-white p-2 text-xs">
              <div className="flex items-center justify-between gap-2">
                <span className="font-mono font-semibold text-slate-800">{call.action}</span>
                <span className={`rounded px-1.5 py-0.5 font-mono ${statusClass(call.status)}`}>
                  {call.status}
                </span>
              </div>

              <div className="mt-1 truncate font-mono text-slate-500" title={call.path}>
                {call.method} {call.path}
              </div>

              <div className="mt-1 flex items-center justify-between gap-2">
                {call.traceId ? (
                  <button
                    type="button"
                    onClick={() => navigator.clipboard.writeText(call.traceId!)}
                    title="Copy trace id"
                    className="truncate font-mono text-sky-700 underline decoration-dotted hover:text-sky-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
                  >
                    {call.traceId}
                  </button>
                ) : (
                  <span className="text-slate-400">no trace id</span>
                )}
                <span className="shrink-0 text-slate-400">{call.durationMs} ms</span>
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
