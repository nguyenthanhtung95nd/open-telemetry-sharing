export const BookmarkKind = { Article: 1, Video: 2, Reference: 3 } as const;
export const BookmarkStatus = { Unread: 1, Read: 2 } as const;

export type BookmarkKindValue = (typeof BookmarkKind)[keyof typeof BookmarkKind];
export type BookmarkStatusValue = (typeof BookmarkStatus)[keyof typeof BookmarkStatus];

export const kindLabels: Record<number, string> = {
  1: "Article",
  2: "Video",
  3: "Reference",
};

export const statusLabels: Record<number, string> = {
  1: "Unread",
  2: "Read",
};

export interface Bookmark {
  id: string;
  title: string;
  url: string;
  description: string | null;
  kind: BookmarkKindValue;
  status: BookmarkStatusValue;
  isArchived: boolean;
  tags: string[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
  lastVisitedAtUtc: string | null;
}

export interface SaveBookmark {
  title: string;
  url: string;
  description: string | null;
  kind: BookmarkKindValue;
  status: BookmarkStatusValue;
  isArchived: boolean;
}

export interface Tag {
  id: string;
  name: string;
  description: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface SaveTag {
  name: string;
  description: string | null;
}

export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface BookmarkQuery {
  q?: string;
  kind?: number;
  status?: number;
  sort?: string;
  page?: number;
  pageSize?: number;
}
