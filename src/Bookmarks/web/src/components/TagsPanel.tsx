import { useState } from "react";
import { useCreateTag, useDeleteTag, useTags } from "../api/hooks";
import { ApiError } from "../api/client";

export function TagsPanel() {
  const [name, setName] = useState("");
  const tags = useTags();
  const create = useCreateTag();
  const remove = useDeleteTag();

  const error = (create.error ?? remove.error) as ApiError | null;

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    await create.mutateAsync({ name, description: null });
    setName("");
  }

  return (
    <section aria-labelledby="tags-heading" className="space-y-3">
      <h2 id="tags-heading" className="text-lg font-semibold text-slate-900">
        Tags
      </h2>

      <form onSubmit={submit} className="flex gap-2">
        <label className="flex-1 text-sm">
          <span className="sr-only">New tag name</span>
          <input
            className="w-full rounded border border-slate-300 px-2 py-1.5 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-sky-600"
            placeholder="New tag, at least 3 characters"
            required
            minLength={3}
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </label>
        <button
          type="submit"
          disabled={create.isPending}
          className="rounded bg-slate-800 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-900 disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
        >
          Add
        </button>
      </form>

      {error && (
        <p role="alert" className="rounded border border-red-200 bg-red-50 p-2 text-sm text-red-800">
          <strong className="font-semibold">{error.status}</strong> {error.message}
        </p>
      )}

      <ul className="flex flex-wrap gap-2">
        {tags.data?.map((tag) => (
          <li
            key={tag.id}
            className="flex items-center gap-1 rounded-full bg-slate-100 py-0.5 pl-3 pr-1 text-sm text-slate-700"
          >
            {tag.name}
            <button
              type="button"
              aria-label={`Delete tag ${tag.name}`}
              onClick={() => remove.mutate(tag.id)}
              className="rounded-full px-1.5 text-slate-500 hover:bg-slate-300 hover:text-slate-900 focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-sky-600"
            >
              ×
            </button>
          </li>
        ))}
      </ul>

      {tags.data?.length === 0 && (
        <p className="text-sm text-slate-400">
          No tags yet. Create one, then try creating it again to see a 409.
        </p>
      )}
    </section>
  );
}
