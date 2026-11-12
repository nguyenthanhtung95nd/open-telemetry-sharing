import { useState } from "react";
import {
  useAssignTags,
  useBookmarks,
  useCreateBookmark,
  useDeleteBookmark,
  useTags,
  useUpdateBookmark,
} from "../api/hooks";
import { ApiError } from "../api/client";
import {
  BookmarkKind,
  BookmarkStatus,
  kindLabels,
  statusLabels,
  type Bookmark,
  type BookmarkQuery,
  type SaveBookmark,
} from "../api/types";

const emptyDraft: SaveBookmark = {
  title: "",
  url: "",
  description: "",
  kind: BookmarkKind.Article,
  status: BookmarkStatus.Unread,
  isArchived: false,
};

const field =
  "w-full rounded border border-slate-300 px-2 py-1.5 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-sky-600";
const primaryButton =
  "rounded bg-sky-700 px-3 py-1.5 text-sm font-medium text-white hover:bg-sky-800 disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600";
const subtleButton =
  "rounded px-2 py-1 text-xs text-slate-600 hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600";

export function BookmarksPanel() {
  const [query, setQuery] = useState<BookmarkQuery>({ page: 1, pageSize: 10 });
  const [draft, setDraft] = useState<SaveBookmark>(emptyDraft);
  const [editingId, setEditingId] = useState<string | null>(null);

  const bookmarks = useBookmarks(query);
  const tags = useTags();
  const create = useCreateBookmark();
  const update = useUpdateBookmark();
  const remove = useDeleteBookmark();
  const assign = useAssignTags();

  const saving = create.isPending || update.isPending;
  const error = (create.error ?? update.error ?? remove.error ?? assign.error) as ApiError | null;

  function startEdit(bookmark: Bookmark) {
    setEditingId(bookmark.id);
    setDraft({
      title: bookmark.title,
      url: bookmark.url,
      description: bookmark.description ?? "",
      kind: bookmark.kind,
      status: bookmark.status,
      isArchived: bookmark.isArchived,
    });
  }

  function reset() {
    setEditingId(null);
    setDraft(emptyDraft);
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    const body: SaveBookmark = { ...draft, description: draft.description || null };
    if (editingId) await update.mutateAsync({ id: editingId, body });
    else await create.mutateAsync(body);
    reset();
  }

  function toggleTag(bookmark: Bookmark, tagName: string, tagId: string) {
    const current = tags.data ?? [];
    const currentIds = current.filter((t) => bookmark.tags.includes(t.name)).map((t) => t.id);
    const next = bookmark.tags.includes(tagName)
      ? currentIds.filter((id) => id !== tagId)
      : [...currentIds, tagId];
    assign.mutate({ id: bookmark.id, tagIds: next });
  }

  return (
    <section aria-labelledby="bookmarks-heading" className="space-y-4">
      <h2 id="bookmarks-heading" className="text-lg font-semibold text-slate-900">
        Bookmarks
      </h2>

      <form onSubmit={submit} className="space-y-3 rounded border border-slate-200 bg-white p-4">
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="block text-sm">
            <span className="mb-1 block font-medium text-slate-700">Title</span>
            <input
              className={field}
              required
              minLength={3}
              value={draft.title}
              onChange={(e) => setDraft({ ...draft, title: e.target.value })}
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block font-medium text-slate-700">URL</span>
            <input
              className={field}
              required
              type="url"
              value={draft.url}
              onChange={(e) => setDraft({ ...draft, url: e.target.value })}
            />
          </label>
        </div>

        <label className="block text-sm">
          <span className="mb-1 block font-medium text-slate-700">Description</span>
          <input
            className={field}
            value={draft.description ?? ""}
            onChange={(e) => setDraft({ ...draft, description: e.target.value })}
          />
        </label>

        <div className="flex flex-wrap items-end gap-3">
          <label className="block text-sm">
            <span className="mb-1 block font-medium text-slate-700">Kind</span>
            <select
              className={field}
              value={draft.kind}
              onChange={(e) => setDraft({ ...draft, kind: Number(e.target.value) as never })}
            >
              {Object.entries(kindLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </label>

          <label className="block text-sm">
            <span className="mb-1 block font-medium text-slate-700">Status</span>
            <select
              className={field}
              value={draft.status}
              onChange={(e) => setDraft({ ...draft, status: Number(e.target.value) as never })}
            >
              {Object.entries(statusLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </label>

          <button type="submit" className={primaryButton} disabled={saving}>
            {editingId ? "Save changes" : "Add bookmark"}
          </button>
          {editingId && (
            <button type="button" onClick={reset} className={subtleButton}>
              Cancel
            </button>
          )}
        </div>
      </form>

      {error && (
        <p role="alert" className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          <strong className="font-semibold">{error.status}</strong> {error.message}
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        <input
          className={`${field} max-w-xs`}
          placeholder="Search title or description"
          value={query.q ?? ""}
          onChange={(e) => setQuery({ ...query, q: e.target.value, page: 1 })}
        />
        <select
          className={`${field} max-w-40`}
          value={query.sort ?? ""}
          onChange={(e) => setQuery({ ...query, sort: e.target.value || undefined })}
        >
          <option value="">Default order</option>
          <option value="title">Title</option>
          <option value="title desc">Title, descending</option>
          <option value="status">Status</option>
        </select>
      </div>

      {bookmarks.isPending && <p className="text-sm text-slate-500">Loading…</p>}
      {bookmarks.isError && (
        <p role="alert" className="text-sm text-red-700">
          {(bookmarks.error as ApiError).message}
        </p>
      )}

      {bookmarks.data && (
        <>
          <ul className="space-y-2">
            {bookmarks.data.items.map((bookmark) => (
              <li key={bookmark.id} className="rounded border border-slate-200 bg-white p-3">
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div className="min-w-0">
                    <a
                      href={bookmark.url}
                      target="_blank"
                      rel="noreferrer"
                      className="font-medium text-sky-800 underline decoration-dotted hover:text-sky-950"
                    >
                      {bookmark.title}
                    </a>
                    <p className="truncate text-xs text-slate-500">{bookmark.url}</p>
                  </div>
                  <div className="flex shrink-0 gap-1">
                    <button type="button" className={subtleButton} onClick={() => startEdit(bookmark)}>
                      Edit
                    </button>
                    <button
                      type="button"
                      className={subtleButton}
                      onClick={() => remove.mutate(bookmark.id)}
                    >
                      Delete
                    </button>
                  </div>
                </div>

                <p className="mt-1 text-xs text-slate-500">
                  {kindLabels[bookmark.kind]} · {statusLabels[bookmark.status]}
                </p>

                {(tags.data?.length ?? 0) > 0 && (
                  <div className="mt-2 flex flex-wrap gap-1">
                    {tags.data!.map((tag) => {
                      const on = bookmark.tags.includes(tag.name);
                      return (
                        <button
                          key={tag.id}
                          type="button"
                          aria-pressed={on}
                          onClick={() => toggleTag(bookmark, tag.name, tag.id)}
                          className={`rounded-full px-2 py-0.5 text-xs focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600 ${
                            on
                              ? "bg-sky-700 text-white"
                              : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                          }`}
                        >
                          {tag.name}
                        </button>
                      );
                    })}
                  </div>
                )}
              </li>
            ))}
          </ul>

          <nav aria-label="Pagination" className="flex items-center gap-3 text-sm">
            <button
              type="button"
              className={subtleButton}
              disabled={!bookmarks.data.hasPreviousPage}
              onClick={() => setQuery({ ...query, page: (query.page ?? 1) - 1 })}
            >
              Previous
            </button>
            <span className="text-slate-500">
              Page {bookmarks.data.page} of {Math.max(bookmarks.data.totalPages, 1)} ·{" "}
              {bookmarks.data.totalCount} total
            </span>
            <button
              type="button"
              className={subtleButton}
              disabled={!bookmarks.data.hasNextPage}
              onClick={() => setQuery({ ...query, page: (query.page ?? 1) + 1 })}
            >
              Next
            </button>
          </nav>
        </>
      )}
    </section>
  );
}
