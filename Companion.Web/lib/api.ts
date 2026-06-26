export type JsonRecord = Record<string, unknown>;

const tokenKey = "companion.accessToken";
const userKey = "companion.currentUser";

export const apiBaseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ??
  "http://localhost:8080";

export function getToken() {
  if (typeof window === "undefined") {
    return null;
  }

  return window.localStorage.getItem(tokenKey);
}

export function setSession(accessToken: string, me: unknown) {
  window.localStorage.setItem(tokenKey, accessToken);
  window.localStorage.setItem(userKey, JSON.stringify(me));
}

export function clearSession() {
  window.localStorage.removeItem(tokenKey);
  window.localStorage.removeItem(userKey);
}

export function getStoredUser<T>() {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.localStorage.getItem(userKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}

export async function apiFetch<T>(
  path: string,
  init: RequestInit = {}
): Promise<T> {
  const token = getToken();
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers,
    cache: "no-store"
  });

  if (response.status === 401) {
    clearSession();
  }

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function login(email: string, password: string) {
  return apiFetch<{
    accessToken: string;
    expiresUtc: string;
    me: CurrentUser;
  }>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password })
  });
}

export type CurrentUser = {
  profile: {
    userId: string;
    userProfileId: string;
    email: string;
    displayName: string;
    roles: string[];
  };
  preferences: unknown[];
  capabilities: JsonRecord;
};

export type PaginatedItem = JsonRecord & {
  id?: string;
  title?: string;
  name?: string;
  subject?: string;
  summary?: string;
  createdUtc?: string;
  updatedUtc?: string;
  status?: string;
  type?: string;
};
