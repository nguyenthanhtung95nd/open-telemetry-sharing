import { BookmarksPanel } from "./components/BookmarksPanel";
import { CallLogPanel } from "./components/CallLogPanel";
import { TagsPanel } from "./components/TagsPanel";

export default function App() {
  return (
    <div className="min-h-dvh bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto max-w-6xl px-4 py-4">
          <h1 className="text-xl font-semibold">Bookmarks</h1>
          <p className="mt-1 text-sm text-slate-600">
            Every request carries an <code className="font-mono">app.action</code> baggage header.
            The API copies it onto all of its spans, so one button press is traceable end to end
            without any OpenTelemetry SDK in the browser.
          </p>
        </div>
      </header>

      <main className="mx-auto grid max-w-6xl gap-8 px-4 py-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <div className="space-y-8">
          <BookmarksPanel />
          <TagsPanel />
        </div>
        <aside className="lg:sticky lg:top-6 lg:h-[calc(100dvh-3rem)]">
          <CallLogPanel />
        </aside>
      </main>
    </div>
  );
}
