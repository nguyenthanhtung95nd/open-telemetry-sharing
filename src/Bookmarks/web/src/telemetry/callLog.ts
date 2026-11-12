import { useSyncExternalStore } from "react";

export interface ApiCall {
  id: number;
  action: string;
  method: string;
  path: string;
  status: number;
  traceId: string | null;
  durationMs: number;
  at: Date;
}

const MAX_ENTRIES = 25;

let calls: ApiCall[] = [];
let nextId = 1;
const listeners = new Set<() => void>();

export function recordCall(call: Omit<ApiCall, "id" | "at">): void {
  calls = [{ ...call, id: nextId++, at: new Date() }, ...calls].slice(0, MAX_ENTRIES);
  listeners.forEach((notify) => notify());
}

export function clearCalls(): void {
  calls = [];
  listeners.forEach((notify) => notify());
}

export function useCallLog(): ApiCall[] {
  return useSyncExternalStore(
    (notify) => {
      listeners.add(notify);
      return () => listeners.delete(notify);
    },
    () => calls,
  );
}
