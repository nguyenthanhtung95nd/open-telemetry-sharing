import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { Bookmark, BookmarkQuery, Page, SaveBookmark, SaveTag, Tag } from "./types";

const BOOKMARKS = "bookmarks";
const TAGS = "tags";

export function useBookmarks(query: BookmarkQuery) {
  return useQuery({
    queryKey: [BOOKMARKS, query],
    queryFn: () =>
      apiFetch<Page<Bookmark>>("/bookmarks", {
        action: "list-bookmarks",
        screen: BOOKMARKS,
        query: { ...query },
      }),
  });
}

export function useTags() {
  return useQuery({
    queryKey: [TAGS],
    queryFn: () => apiFetch<Tag[]>("/tags", { action: "list-tags", screen: TAGS }),
  });
}

export function useCreateBookmark() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (body: SaveBookmark) =>
      apiFetch<Bookmark>("/bookmarks", {
        action: "create-bookmark",
        screen: BOOKMARKS,
        method: "POST",
        body,
      }),
    onSuccess: () => client.invalidateQueries({ queryKey: [BOOKMARKS] }),
  });
}

export function useUpdateBookmark() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: SaveBookmark }) =>
      apiFetch<Bookmark>(`/bookmarks/${id}`, {
        action: "update-bookmark",
        screen: BOOKMARKS,
        method: "PUT",
        body,
      }),
    onSuccess: () => client.invalidateQueries({ queryKey: [BOOKMARKS] }),
  });
}

export function useDeleteBookmark() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiFetch<void>(`/bookmarks/${id}`, {
        action: "delete-bookmark",
        screen: BOOKMARKS,
        method: "DELETE",
      }),
    onSuccess: () => client.invalidateQueries({ queryKey: [BOOKMARKS] }),
  });
}

export function useAssignTags() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: ({ id, tagIds }: { id: string; tagIds: string[] }) =>
      apiFetch<void>(`/bookmarks/${id}/tags`, {
        action: "assign-bookmark-tags",
        screen: BOOKMARKS,
        method: "PUT",
        body: { tagIds },
      }),
    onSuccess: () => client.invalidateQueries({ queryKey: [BOOKMARKS] }),
  });
}

export function useCreateTag() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (body: SaveTag) =>
      apiFetch<Tag>("/tags", { action: "create-tag", screen: TAGS, method: "POST", body }),
    onSuccess: () => client.invalidateQueries({ queryKey: [TAGS] }),
  });
}

export function useDeleteTag() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiFetch<void>(`/tags/${id}`, { action: "delete-tag", screen: TAGS, method: "DELETE" }),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: [TAGS] });
      client.invalidateQueries({ queryKey: [BOOKMARKS] });
    },
  });
}
