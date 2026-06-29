import AsyncStorage from "@react-native-async-storage/async-storage";
import Constants from "expo-constants";

const tokenKey = "companion.mobile.accessToken";
const userKey = "companion.mobile.currentUser";
const apiBaseUrlKey = "companion.mobile.apiBaseUrl";
const biometricKey = "companion.mobile.biometricEnabled";

const defaultApiBaseUrl =
  normalizeApiBaseUrl(process.env.EXPO_PUBLIC_API_BASE_URL) ??
  normalizeApiBaseUrl(Constants.expoConfig?.extra?.apiBaseUrl as string | undefined) ??
  "http://100.71.8.121:8080";

export type JsonRecord = Record<string, unknown>;

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

export type MobileSession = {
  accessToken: string;
  me: CurrentUser;
  apiBaseUrl: string;
};

export function normalizeApiBaseUrl(value?: string | null) {
  const trimmed = value?.trim();
  if (!trimmed) {
    return null;
  }

  return trimmed.replace(/\/$/, "");
}

export async function getApiBaseUrl() {
  return (await AsyncStorage.getItem(apiBaseUrlKey)) ?? defaultApiBaseUrl;
}

export async function setApiBaseUrl(value: string) {
  await AsyncStorage.setItem(apiBaseUrlKey, normalizeApiBaseUrl(value) ?? defaultApiBaseUrl);
}

export async function getStoredSession(): Promise<MobileSession | null> {
  const [accessToken, rawUser, apiBaseUrl] = await Promise.all([
    AsyncStorage.getItem(tokenKey),
    AsyncStorage.getItem(userKey),
    getApiBaseUrl()
  ]);

  if (!accessToken || !rawUser) {
    return null;
  }

  try {
    return {
      accessToken,
      me: JSON.parse(rawUser) as CurrentUser,
      apiBaseUrl
    };
  } catch {
    await clearSession();
    return null;
  }
}

export async function setSession(accessToken: string, me: CurrentUser, apiBaseUrl: string) {
  await Promise.all([
    AsyncStorage.setItem(tokenKey, accessToken),
    AsyncStorage.setItem(userKey, JSON.stringify(me)),
    setApiBaseUrl(apiBaseUrl)
  ]);
}

export async function clearSession() {
  await Promise.all([
    AsyncStorage.removeItem(tokenKey),
    AsyncStorage.removeItem(userKey)
  ]);
}

export async function isBiometricEnabled() {
  return (await AsyncStorage.getItem(biometricKey)) === "true";
}

export async function setBiometricEnabled(enabled: boolean) {
  await AsyncStorage.setItem(biometricKey, enabled ? "true" : "false");
}

export async function login(apiBaseUrl: string, email: string, password: string) {
  const normalizedBaseUrl = normalizeApiBaseUrl(apiBaseUrl) ?? defaultApiBaseUrl;
  const response = await fetch(`${normalizedBaseUrl}/api/auth/login`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ email, password })
  });

  if (!response.ok) {
    throw new Error(parseErrorMessage(await response.text()) || "Login failed");
  }

  return (await response.json()) as {
    accessToken: string;
    expiresUtc: string;
    me: CurrentUser;
  };
}

export async function logout(session: MobileSession) {
  await apiFetch<void>(session, "/api/auth/logout", {
    method: "POST"
  });
}

export async function apiFetch<T>(
  session: MobileSession,
  path: string,
  init: RequestInit = {}
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  headers.set("Authorization", `Bearer ${session.accessToken}`);

  const response = await fetch(`${session.apiBaseUrl}${path}`, {
    ...init,
    headers
  });

  if (response.status === 401) {
    await clearSession();
  }

  if (!response.ok) {
    throw new Error(parseErrorMessage(await response.text()) || `${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function parseErrorMessage(text: string) {
  if (!text) {
    return "";
  }

  try {
    const parsed = JSON.parse(text) as {
      message?: unknown;
      title?: unknown;
      detail?: unknown;
      errors?: Record<string, unknown>;
    };
    if (typeof parsed.message === "string") {
      return parsed.message;
    }

    if (typeof parsed.detail === "string") {
      return parsed.detail;
    }

    if (parsed.errors && typeof parsed.errors === "object") {
      const values = Object.values(parsed.errors).flatMap((value) =>
        Array.isArray(value) ? value.map(String) : [String(value)]
      );
      if (values.length > 0) {
        return values.join("\n");
      }
    }

    if (typeof parsed.title === "string") {
      return parsed.title;
    }
  } catch {
    return text;
  }

  return text;
}

export async function cachedApiFetch<T>(
  session: MobileSession,
  path: string,
  cacheKey: string
): Promise<{ data: T; fromCache: boolean }> {
  try {
    const data = await apiFetch<T>(session, path);
    await AsyncStorage.setItem(cacheKey, JSON.stringify(data));
    return { data, fromCache: false };
  } catch (error) {
    const cached = await AsyncStorage.getItem(cacheKey);
    if (!cached) {
      throw error;
    }

    return {
      data: JSON.parse(cached) as T,
      fromCache: true
    };
  }
}
